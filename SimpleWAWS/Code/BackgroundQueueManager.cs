using Kudu.Client.Zip;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;

namespace SimpleWAWS.Code
{
    public class BackgroundQueueManager
    {

        public readonly ConcurrentQueue<ResourceGroup> FreeResourceGroups = new ConcurrentQueue<ResourceGroup>();

        public IEnumerable<ResourceGroup> LoadedResourceGroups
        {
            get
            {
                return FreeResourceGroups.ToList()
                       .Concat(FreeLinuxResourceGroups.ToList())
                       .Concat(ResourceGroupsInUse.Where(e => e.Value != null).Select(e => e.Value))
                       .Concat(ResourceGroupsInProgress.Where(e => e.Value != null).Select(e => e.Value.ResourceGroup)); 
            }
        }
        public readonly ConcurrentQueue<ResourceGroup> FreeLinuxResourceGroups = new ConcurrentQueue<ResourceGroup>();
        public readonly ConcurrentDictionary<string, InProgressOperation> ResourceGroupsInProgress = new ConcurrentDictionary<string, InProgressOperation>();
        public readonly ConcurrentDictionary<string, ResourceGroup> ResourceGroupsInUse = new ConcurrentDictionary<string, ResourceGroup>();
        public readonly ConcurrentDictionary<Guid, BackgroundOperation> BackgroundInternalOperations = new ConcurrentDictionary<Guid, BackgroundOperation>();
        private Timer _logQueueStatsTimer;
        private Timer _cleanupSubscriptionsTimer;
        private readonly JobHost _jobHost = new JobHost();
        private static int _maintainResourceGroupListErrorCount = 0;
        private static Random rand = new Random();

        public BackgroundQueueManager()
        {
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
                            FreeLinuxResourceGroups.Enqueue(readyToAddRg);
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
                    var resources = this.ResourceGroupsInUse
                    .Select(e => e.Value)
                    .Where(rg => (int)rg.TimeLeft.TotalSeconds == 0);

                    foreach (var resource in resources)
                    {
                        DeleteResourceGroup(resource);
                    }

                    var subscriptions = await CsmManager.GetSubscriptions();
                    var totalDeletedRGs = 0;
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    foreach (var sub in subscriptions)
                    {
                        var s = await new Subscription(sub).Load(false);
                        SimpleTrace.Diagnostics.Information($"Deleting resources in {s.Type} subscription {s.SubscriptionId}");

                        var csmResourceGroups = await s.LoadResourceGroupsForSubscription();
                        var deleteExtras = csmResourceGroups.value
                            .Where(p => ! LoadedResourceGroups.Any(p2 => string.Equals(p.id, p2.CsmId, StringComparison.OrdinalIgnoreCase))).GroupBy(rg => rg.location)
                            .Select(g => new { Region = g.Key, ResourceGroups = g.Select(r => r), Count = g.Count() })
                            .Where(g => g.Count > s.ResourceGroupsPerGeoRegion)
                            .Select(g => g.ResourceGroups.Where(rg => !rg.tags.ContainsKey("UserId")))
                            .SelectMany(i => i);
                        
                        totalDeletedRGs += deleteExtras.Count();
                        AppInsights.TelemetryClient.TrackMetric("deletedRGs", deleteExtras.Count());
                        Parallel.ForEach(deleteExtras, async (resourceGroup) =>
                        {
                            try
                            {
                                var georegion = CsmManager.RegionHashTable[resourceGroup.location].ToString();
                                SimpleTrace.Diagnostics.Information($"Deleting leaked {georegion} resource  {resourceGroup.name}");
                                await new ResourceGroup(s.SubscriptionId, resourceGroup.name, georegion).Delete(false);
                            }
                            catch (Exception ex)
                            {
                                SimpleTrace.Diagnostics.Error($"Leaking RG Delete Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
                            }
                        });
                    }

                    AppInsights.TelemetryClient.TrackMetric("leakingCleanupTime", sw.Elapsed.TotalSeconds);
                    //Delete any duplicate resourcegroups in same subscription loaded in the same region
                    //or create any missing resourcegroups in a region
                    IList<Tuple<string, MakeSubscriptionFreeTrialResult>> subscriptionStates = new List<Tuple<string, MakeSubscriptionFreeTrialResult>>();
                    foreach (var subscription in subscriptions)
                    {
                        var sub = new Subscription(subscription);
                        sub.ResourceGroups = LoadedResourceGroups.Where(r => r.SubscriptionId == sub.SubscriptionId);
                        var trialsubresult = sub.MakeTrialSubscription();
                        subscriptionStates.Add(new Tuple<string, MakeSubscriptionFreeTrialResult>(subscription, trialsubresult));
                    }
                    foreach (var subscriptionState in subscriptionStates)
                    {
                        foreach (var geoRegion in subscriptionState.Item2.ToCreateInRegions)
                        {
                            CreateResourceGroupOperation(subscriptionState.Item1, geoRegion);
                        }
                        foreach (var resourceGroup in subscriptionState.Item2.ToDelete)
                        {
                            RemoveFromFreeQueue(resourceGroup);
                            DeleteResourceGroupOperation(resourceGroup);
                        }
                    }
                    AppInsights.TelemetryClient.TrackMetric("fullCleanupTime", sw.Elapsed.TotalSeconds);
                    sw.Stop();
                });
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, "CleanupSubscriptions error, Count {Count}", Interlocked.Increment(ref _maintainResourceGroupListErrorCount));
            }

        }

        private void RemoveFromFreeQueue(ResourceGroup resourceGroup)
        {
            switch (resourceGroup.SubscriptionType)
            {
                case SubscriptionType.AppService:
                    RemoveFromFreeAppServiceQueue(resourceGroup);
                    break;
                case SubscriptionType.Linux:
                    RemoveFromFreeLinuxQueue(resourceGroup);
                    break;
                default:
                    SimpleTrace.Diagnostics.Warning($"Resourcegroup subscriptiontype cannot be determined {resourceGroup.CsmId}");
                    break;
            } 
        }

        private void RemoveFromFreeAppServiceQueue(ResourceGroup resourceGroup)
        {
            if (this.FreeResourceGroups.ToList().Any(r => string.Equals(r.CsmId, resourceGroup.CsmId, StringComparison.OrdinalIgnoreCase)))
            {
                var dequeueCount = this.FreeResourceGroups.Count;
                ResourceGroup temp;
                while (dequeueCount-- >= 0 && (this.FreeResourceGroups.TryDequeue(out temp)))
                {
                    if (string.Equals(temp.ResourceGroupName, resourceGroup.ResourceGroupName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    else
                    {
                        this.FreeResourceGroups.Enqueue(temp);
                    }
                }
            }
        }
        private void RemoveFromFreeLinuxQueue(ResourceGroup resourceGroup)
        {
            if (this.FreeLinuxResourceGroups.ToList().Any(r => string.Equals(r.CsmId, resourceGroup.CsmId, StringComparison.OrdinalIgnoreCase)))
            {
                var dequeueCount = this.FreeLinuxResourceGroups.Count;
                ResourceGroup temp;
                while (dequeueCount-- >= 0 && (this.FreeLinuxResourceGroups.TryDequeue(out temp)))
                {
                    if (string.Equals(temp.ResourceGroupName, resourceGroup.ResourceGroupName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    else
                    {
                        this.FreeLinuxResourceGroups.Enqueue(temp);
                    }
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
                Description = $"Logging usage statistics for resourceGroup {resourceGroup.CsmId}",
                Type = OperationType.LogUsageStatistics,
                Task = LogActiveUsageStatistics(resourceGroup),
                RetryAction = () => LogUsageStatistics(resourceGroup)
            });
        }

        private void DeleteAndCreateResourceGroupOperation(ResourceGroup resourceGroup)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Deleting and creating resourceGroup {resourceGroup.CsmId}",
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
            var freeLinuxResources = FreeLinuxResourceGroups.Count(sub => sub.SubscriptionType == SubscriptionType.Linux);
            var inUseLinuxResources = ResourceGroupsInUse.Select(s => s.Value).Count(sub => sub.SubscriptionType == SubscriptionType.Linux);

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
            AppInsights.TelemetryClient.TrackMetric("freeLinuxResources", freeLinuxResources);
            AppInsights.TelemetryClient.TrackMetric("inUseLinuxResources", inUseLinuxResources);
        }

        private void DeleteResourceGroupOperation(ResourceGroup resourceGroup)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Deleting resourceGroup {resourceGroup.CsmId}",
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
                Description = $"Putting resourceGroup {resourceGroup.CsmId} in desired state",
                Type = OperationType.ResourceGroupPutInDesiredState,
                Task = resourceGroup.PutInDesiredState(),
                RetryAction = () => PutResourceGroupInDesiredStateOperation(resourceGroup)
            });
        }

        private void AddOperation<T>(BackgroundOperation<T> operation)
        {
            if (BackgroundInternalOperations.Count > SimpleSettings.BackGroundQueueSize)
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