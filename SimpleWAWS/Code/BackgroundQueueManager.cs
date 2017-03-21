using Kudu.Client.Zip;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleWAWS.Code
{
    public class BackgroundQueueManager
    {

        public readonly ConcurrentQueue<ResourceGroup> FreeResourceGroups = new ConcurrentQueue<ResourceGroup>();
        public readonly ConcurrentQueue<ResourceGroup> FreeJenkinsResourceGroups = new ConcurrentQueue<ResourceGroup>();
        public readonly ConcurrentDictionary<string, InProgressOperation> ResourceGroupsInProgress = new ConcurrentDictionary<string, InProgressOperation>();
        public readonly ConcurrentDictionary<string, ResourceGroup> ResourceGroupsInUse = new ConcurrentDictionary<string, ResourceGroup>();
        public readonly ConcurrentDictionary<Guid, BackgroundOperation> BackgroundInternalOperations = new ConcurrentDictionary<Guid, BackgroundOperation>();
        private Timer _maintainResourceGroupTimer;
        private Timer _logQueueStatsTimer;
        private Timer _cleanupSubscriptionsTimer;
        private readonly JobHost _jobHost = new JobHost();
        private static int _maintainResourceGroupListErrorCount = 0;
        private static Random rand = new Random();

        public BackgroundQueueManager()
        {
            if (_maintainResourceGroupTimer == null)
            {
                _maintainResourceGroupTimer = new Timer(OnMaintainResourceGroupTimerElapsed, null, TimeSpan.FromMinutes(SimpleSettings.MaintainResourceGroupsMinutes), TimeSpan.FromMinutes(SimpleSettings.MaintainResourceGroupsMinutes));
            }
            if (_logQueueStatsTimer == null)
            {
                _logQueueStatsTimer = new Timer(OnLogQueueStatsTimerElapsed, null, TimeSpan.FromMinutes(SimpleSettings.LoqQueueStatsMinutes), TimeSpan.FromMinutes(SimpleSettings.LoqQueueStatsMinutes));
            }
            if (_cleanupSubscriptionsTimer == null)
            {
                _cleanupSubscriptionsTimer = new Timer(OnCleanupSubscriptionsTimerElapsed, null, TimeSpan.FromMinutes(SimpleSettings.CleanupSubscriptionMinutes), TimeSpan.FromMinutes(SimpleSettings.CleanupSubscriptionMinutes));
            }
        }

        public void LoadSubscription(string subscriptionId)
        {
            var subscription = new Subscription(subscriptionId);
            AddOperation(new BackgroundOperation<Subscription>
            {
                Description = $"Loading subscription {subscriptionId}",
                Type = OperationType.SubscriptionLoad,
                //Moving bad resourcegroup deletion to background thread. No need to wait on this 
                //during app startup
                Task = subscription.Load(deleteBadResourceGroups : false),
                RetryAction = () => LoadSubscription(subscriptionId)
            });
        }

        public void DeleteResourceGroup(ResourceGroup resourceGroup)
        {
            ResourceGroup temp;
            if (this.ResourceGroupsInUse.TryRemove(resourceGroup.UserId, out temp))
            {
                LogUsageStatistics(resourceGroup);
            }
        }

        private async Task<ResourceGroup> LogActiveUsageStatistics(ResourceGroup resourceGroup)
        {
            if (resourceGroup.SubscriptionType == SubscriptionType.AppService)
            {
                try
                {
                    var site = resourceGroup.Sites.FirstOrDefault(s => resourceGroup.SubscriptionType == SubscriptionType.AppService
                            ? s.IsSimpleWAWSOriginalSite
                            : s.IsFunctionsContainer);
                    if (site == null) throw new ArgumentNullException(nameof(site));
                    var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                    var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials);

                    using (var httpContentStream = await zipManager.GetZipFileStreamAsync("LogFiles/http/RawLogs"))
                    {
                        await StorageHelper.UploadBlob(resourceGroup.ResourceUniqueId, httpContentStream);
                    }
                    await StorageHelper.AddQueueMessage(new { BlobName = resourceGroup.ResourceUniqueId });
                    SimpleTrace.TraceInformation("{0}; {1}", AnalyticsEvents.SiteIISLogsName, resourceGroup.ResourceUniqueId);
                }
                catch (Exception e)
                {
                    if (!(e is HttpRequestException))
                    {
                        SimpleTrace.Diagnostics.Error(e, "Error logging active usage numbers");
                    }
                }
            }
            return resourceGroup;
        }

        private void HandleBackgroundOperation(BackgroundOperation operation)
        {
            BackgroundOperation temp;
            if (!BackgroundInternalOperations.TryRemove(operation.OperationId, out temp)) return;

            var subTask = temp as BackgroundOperation<Subscription>;
            var resourceGroupTask = temp as BackgroundOperation<ResourceGroup>;

            if ((subTask != null && subTask.Task.IsFaulted) ||
                (resourceGroupTask != null && resourceGroupTask.Task.IsFaulted))
            {
                //temp.RetryAction();
                SimpleTrace.Diagnostics.Fatal("Losing ResourceGroup with {@exception}", resourceGroupTask?.Task?.Exception);
                return;
            }

            switch (operation.Type)
            {
                case OperationType.SubscriptionLoad:
                    var subscription = subTask.Task.Result;
                    var result = subscription.MakeTrialSubscription();
                    foreach (var resourceGroup in result.Ready)
                    {
                        PutResourceGroupInDesiredStateOperation(resourceGroup);
                    }
                    foreach (var geoRegion in result.ToCreateInRegions)
                    {
                        CreateResourceGroupOperation(subscription.SubscriptionId, geoRegion);
                    }
                    foreach (var resourceGroup in result.ToDelete)
                    {
                        DeleteResourceGroupOperation(resourceGroup);
                    }
                    break;

                case OperationType.ResourceGroupPutInDesiredState:
                case OperationType.ResourceGroupDeleteThenCreate:
                    var readyToAddRg = resourceGroupTask.Task.Result;
                    if (readyToAddRg.UserId != null)
                    {
                        if (!ResourceGroupsInUse.TryAdd(readyToAddRg.UserId, readyToAddRg))
                        {
                            DeleteAndCreateResourceGroupOperation(readyToAddRg);
                        }
                    }
                    else
                    {
                        if (readyToAddRg.SubscriptionType == SubscriptionType.AppService)
                        {
                            FreeResourceGroups.Enqueue(readyToAddRg);
                        }
                        else
                        {
                            FreeJenkinsResourceGroups.Enqueue(readyToAddRg);
                        }
                    }
                    break;

                case OperationType.ResourceGroupCreate:
                    PutResourceGroupInDesiredStateOperation(resourceGroupTask.Task.Result);
                    break;

                case OperationType.LogUsageStatistics:
                    DeleteAndCreateResourceGroupOperation(resourceGroupTask.Task.Result);
                    break;

                case OperationType.ResourceGroupDelete:
                default:
                    break;
            }
        }

        private void OnMaintainResourceGroupTimerElapsed(object state)
        {
            try
            {
                _jobHost.DoWork(async () =>
                {
                    await MaintainResourceGroupLists();
                });
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, "MaintainResourceGroupLists error, Count {Count}", Interlocked.Increment(ref _maintainResourceGroupListErrorCount));
            }

        }

        private void OnLogQueueStatsTimerElapsed(object state)
        {
            try
            {
                _jobHost.DoWork(() =>
                {
                    LogQueueStatistics();
                });
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, "LogQueueStatistics error, Count {Count}", Interlocked.Increment(ref _maintainResourceGroupListErrorCount));
            }
        }

        private void OnCleanupSubscriptionsTimerElapsed(object state)
        {
            try
            {
                _jobHost.DoWork(async () =>
                {
                    if (!this.BackgroundInternalOperations.Any((a) => a.Value.Type == OperationType.SubscriptionLoad))
                    {
                        foreach (var subcription in await CsmManager.GetSubscriptions())
                        {
                            {
                                SubscriptionCleanup(new Subscription(subcription));
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, "CleanupSubscriptions error, Count {Count}", Interlocked.Increment(ref _maintainResourceGroupListErrorCount));
            }

        }
        private async Task MaintainResourceGroupLists()
        {
            var resources = this.ResourceGroupsInUse
                .Select(e => e.Value)
                .Where(rg => (int)rg.TimeLeft.TotalSeconds == 0);

            foreach (var resource in resources)
            {
                DeleteResourceGroup(resource);
            }

            //Delete any duplicate resourcegroups in same subscription loaded in the same region
            //or create any missing resourcegroups in a region
            IList<Tuple<string,MakeSubscriptionFreeTrialResult>> subscriptionStates = new List<Tuple<string, MakeSubscriptionFreeTrialResult>>();
            foreach (var subscription in await CsmManager.GetSubscriptions())
            {
                var trialsubresult = new Subscription(subscription).MakeTrialSubscription();
                subscriptionStates.Add(new Tuple<string, MakeSubscriptionFreeTrialResult>(subscription, trialsubresult));
            }
            foreach (var state in subscriptionStates)
            {
                foreach (var georegion in state.Item2.ToCreateInRegions)
                {
                    CreateResourceGroupOperation(state.Item1, georegion);
                }
                foreach (var georegion in state.Item2.ToDelete)
                {
                    DeleteResourceGroupOperation(georegion);
                }
            }

        }

        private void SubscriptionCleanup(Subscription subscription)
        {
            AddOperation(new BackgroundOperation<Subscription>
            {
                Description = $"Cleaning subscriptions",
                Type = OperationType.SubscriptionCleanup,
                Task = CsmManager.SubscriptionCleanup(subscription),
                RetryAction = () => SubscriptionCleanup(subscription)
            });
        }
        private void LogUsageStatistics(ResourceGroup resourceGroup)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Logging usage statistics for resourceGroup {resourceGroup.ResourceGroupName}",
                Type = OperationType.LogUsageStatistics,
                Task = LogActiveUsageStatistics(resourceGroup),
                RetryAction = () => LogUsageStatistics(resourceGroup)
            });
        }

        private void DeleteAndCreateResourceGroupOperation(ResourceGroup resourceGroup)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Deleting and creating resourceGroup {resourceGroup.ResourceGroupName}",
                Type = OperationType.ResourceGroupDeleteThenCreate,
                Task = resourceGroup.DeleteAndCreateReplacement(blockDelete: false),
                RetryAction = () => DeleteAndCreateResourceGroupOperation(resourceGroup)
            });
        }

        private void LogQueueStatistics()
        {
            AppInsights.TelemetryClient.TrackEvent("StartLoggingQueueStats", null);
            var freeSitesCount = FreeResourceGroups.Count(sub => sub.SubscriptionType == SubscriptionType.AppService);
            var inUseSites = ResourceGroupsInUse.Select(s => s.Value).Where(sub => sub.SubscriptionType == SubscriptionType.AppService);
            var resourceGroups = inUseSites as IList<ResourceGroup> ?? inUseSites.ToList();
            var inUseSitesCount = resourceGroups.Count();
            var inUseFunctionsCount = resourceGroups.Count(res => res.AppService == AppService.Function);
            var inUseWebsitesCount = resourceGroups.Count(res => res.AppService == AppService.Web);
            var inUseMobileCount = resourceGroups.Count(res => res.AppService == AppService.Mobile);
            var inUseLogicAppCount = resourceGroups.Count(res => res.AppService == AppService.Logic);
            var inUseApiAppCount = resourceGroups.Count(res => res.AppService == AppService.Api);
            var inProgress = ResourceGroupsInProgress.Select(s => s.Value).Count();
            var backgroundOperations = BackgroundInternalOperations.Select(s => s.Value).Count();
            var freeJenkinsResources = FreeJenkinsResourceGroups.Count(sub => sub.SubscriptionType == SubscriptionType.Jenkins);
            var inUseJenkinsResources = ResourceGroupsInUse.Select(s => s.Value).Count(sub => sub.SubscriptionType == SubscriptionType.Jenkins);

            AppInsights.TelemetryClient.TrackMetric("freeSites", freeSitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseSites", inUseSitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseFunctionsCount", inUseFunctionsCount);
            AppInsights.TelemetryClient.TrackMetric("inUseWebsitesCount", inUseWebsitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseMobileCount", inUseMobileCount);
            AppInsights.TelemetryClient.TrackMetric("inUseLogicAppCount", inUseLogicAppCount);
            AppInsights.TelemetryClient.TrackMetric("inUseApiAppCount", inUseApiAppCount);
            AppInsights.TelemetryClient.TrackMetric("inUseSites", inUseSitesCount);

            AppInsights.TelemetryClient.TrackMetric("inProgressOperations", inProgress);
            AppInsights.TelemetryClient.TrackMetric("backgroundOperations", backgroundOperations);
            AppInsights.TelemetryClient.TrackMetric("freeJenkinsResources", freeJenkinsResources);
            AppInsights.TelemetryClient.TrackMetric("inUseJenkinsResources", inUseJenkinsResources);

        }

        private void DeleteResourceGroupOperation(ResourceGroup resourceGroup)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Deleting resourceGroup {resourceGroup.ResourceGroupName}",
                Type = OperationType.ResourceGroupDelete,
                Task = resourceGroup.Delete(false),
                RetryAction = () => DeleteResourceGroupOperation(resourceGroup)
            });
        }

        private void CreateResourceGroupOperation(string subscriptionId, string geoRegion)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Creating resourceGroup in {subscriptionId} in {geoRegion}",
                Type = OperationType.ResourceGroupCreate,
                Task = CsmManager.CreateResourceGroup(subscriptionId, geoRegion),
                RetryAction = () => CreateResourceGroupOperation(subscriptionId, geoRegion)
            });
        }

        private void PutResourceGroupInDesiredStateOperation(ResourceGroup resourceGroup)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Putting resourceGroup {resourceGroup.ResourceGroupName} in desired state",
                Type = OperationType.ResourceGroupPutInDesiredState,
                Task = resourceGroup.PutInDesiredState(),
                RetryAction = () => PutResourceGroupInDesiredStateOperation(resourceGroup)
            });
        }

        private void AddOperation<T>(BackgroundOperation<T> operation)
        {
            if (BackgroundInternalOperations.Count > 70)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(rand.Next(1000, 5000));
                    AddOperation(operation);
                });
            }
            else
            {
                BackgroundInternalOperations.TryAdd(operation.OperationId, operation);
                operation.Task.ContinueWith(_ => HandleBackgroundOperation(operation)).ConfigureAwait(false);
            }
        }
    }
}