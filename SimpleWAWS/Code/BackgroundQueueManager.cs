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
        public static bool IsWriteInstance = String.Equals(SimpleSettings.WEBSITE_HOME_STAMPNAME, SimpleSettings.BackgroundQueueManagerStampName, StringComparison.OrdinalIgnoreCase);
        //public readonly ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>> FreeResourceGroups = new ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>>();
        //public readonly ConcurrentDictionary<string, ResourceGroup> ResourceGroupsInUse = new ConcurrentDictionary<string, ResourceGroup>();

        public async Task<IEnumerable<ResourceGroup>> LoadedResourceGroups()
        {
                var list = new List<ResourceGroup>();
                list.AddRange(await StorageHelper.GetAllFreeResources());
                list.AddRange((await StorageHelper.GetInUseResourceGroups()).Where(e => e.Value != null).Select(e => e.Value));
                list.AddRange(ResourceGroupsInProgress.Where(e => e.Value != null).Select(e => e.Value.ResourceGroup));
                return list;
        }
        public readonly ConcurrentDictionary<string, InProgressOperation> ResourceGroupsInProgress = new ConcurrentDictionary<string, InProgressOperation>();
        public readonly ConcurrentDictionary<Guid, BackgroundOperation> BackgroundInternalOperations = new ConcurrentDictionary<Guid, BackgroundOperation>();
        private Timer _logQueueStatsTimer;
        private Timer _cleanupSubscriptionsTimer;
        private Timer _cleanupExpiredResourceGroupsTimer;
        private readonly JobHost _jobHost = new JobHost();
        private static int _maintainResourceGroupListErrorCount = 0;
        private static Random rand = new Random();
        internal Stopwatch _uptime;
        internal int _cleanupOperationsTriggered;

        public BackgroundQueueManager()
        {
            if (IsWriteInstance)
            {
                if (_logQueueStatsTimer == null)
                {
                    _logQueueStatsTimer = new Timer(OnLogQueueStatsTimerElapsed, null, TimeSpan.FromMinutes(SimpleSettings.LoqQueueStatsMinutes), TimeSpan.FromMinutes(SimpleSettings.LoqQueueStatsMinutes));
                }
                if (_cleanupSubscriptionsTimer == null)
                {
                    _cleanupSubscriptionsTimer = new Timer(OnCleanupSubscriptionsTimerElapsed, null, TimeSpan.FromMinutes(SimpleSettings.CleanupSubscriptionFirstInvokeMinutes), TimeSpan.FromMinutes(SimpleSettings.CleanupSubscriptionMinutes));
                }
                if (_cleanupExpiredResourceGroupsTimer == null)
                {
                    _cleanupExpiredResourceGroupsTimer = new Timer(OnExpiredResourceGroupsTimerElapsed, null, TimeSpan.FromMinutes(SimpleSettings.CleanupExpiredResourceGroupsMinutes), TimeSpan.FromMinutes(SimpleSettings.CleanupExpiredResourceGroupsMinutes));
                }
                if (_uptime == null)
                {
                    _uptime = Stopwatch.StartNew();
                }
            }
        }

        public void LoadSubscription(string subscriptionId)
        {
            var subscription = new Subscription(subscriptionId);
            AddOperation(new BackgroundOperation<Subscription>
            {
                Description = $"Loading subscription {subscriptionId}",
                Type = OperationType.SubscriptionLoad,
                Task = subscription.Load(),
                RetryAction = () => LoadSubscription(subscriptionId)
            });
        }

        public async Task DeleteResourceGroup(ResourceGroup resourceGroup)
        {
            ///TODO: Add checks to ensure this is being called only if there RG has expired
            if ((await StorageHelper.UnAssignResourceGroup(resourceGroup.UserId)))
            {
                SimpleTrace.TraceInformation("resourcegroup removed for user {0}", resourceGroup.UserId);
            }
            else
            {
                SimpleTrace.TraceError("No resourcegroup checked out for {0}", resourceGroup.UserId);
            }
        }

        private async Task HandleBackgroundOperation(BackgroundOperation operation)
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
                    var result = subscription.GetSubscriptionStats();
                    foreach (var resourceGroup in result.Ready)
                    {
                        PutResourceGroupInDesiredStateOperation(resourceGroup);
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
                        if (await StorageHelper.AssignResourceGroup(readyToAddRg.UserId, readyToAddRg))
                        {
                            DeleteAndCreateResourceGroupOperation(readyToAddRg);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(readyToAddRg.DeployedTemplateName) && !string.IsNullOrEmpty(readyToAddRg.SiteGuid))
                        {
                            //if (!FreeResourceGroups.ContainsKey(readyToAddRg.DeployedTemplateName))
                            //{
                            //    readyToAddRg.TemplateName = readyToAddRg.DeployedTemplateName;
                            //    FreeResourceGroups.GetOrAdd(readyToAddRg.DeployedTemplateName, new ConcurrentQueue<ResourceGroup>());
                            //}
                            readyToAddRg.TemplateName = readyToAddRg.DeployedTemplateName;
                            await StorageHelper.AddQueueMessage(readyToAddRg.QueueName, readyToAddRg);
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
        private void OnExpiredResourceGroupsTimerElapsed(object state)
        {
            try
            {
                _jobHost.DoWork(async () =>
                {
                    var resources = (await StorageHelper.GetInUseResourceGroups())
                        .Select(e => e.Value)
                        .Where(rg => (int)rg.TimeLeft.TotalSeconds == 0);

                    foreach (var resource in resources)
                    {
                        await DeleteResourceGroup(resource);
                    }

                });
            }
            catch
            {
            }
        }
        private void OnCleanupSubscriptionsTimerElapsed(object state)
        {

            _jobHost.DoWork(async () =>
            {
                await CleanupSubscriptions();
            });
        }

        internal async Task CleanupSubscriptions()
        {
            try
            {
                _cleanupOperationsTriggered++;
                var subscriptions = await CsmManager.GetSubscriptions();
                Stopwatch sw = new Stopwatch();
                sw.Start();

                //Delete any duplicate resourcegroups in same subscription loaded in the same region
                //or create any missing resourcegroups in a region
                List<ResourceGroup> toDelete = new List<ResourceGroup>();
                List<ResourceGroup> ready= new List<ResourceGroup>();

                var deletedDuplicateRGs = 0;
                var createdMissingRGs = 0;
                var createdMissingTemplates = 0;
                foreach (var subscription in subscriptions)
                {
                    var sub = new Subscription(subscription);
                    sub.ResourceGroups = (await LoadedResourceGroups()).Where(r => r.SubscriptionId == sub.SubscriptionId);
                    var trialsubresult = sub.GetSubscriptionStats();
                    toDelete.AddRange(trialsubresult.ToDelete);
                    ready.AddRange(trialsubresult.Ready);
                }

                var rand = new Random();
                foreach (var resourceGroup in toDelete)
                {
                    //RemoveFromFreeResourcesQueue(resourceGroup);
                    DeleteResourceGroupOperation(resourceGroup);
                    deletedDuplicateRGs++;
                }
                foreach (var template in TemplatesManager.GetTemplates().Where(a => a.QueueSizeToMaintain > 0))
                {
                    var deployedTemplates = ready.Count(a => a.DeployedTemplateName == template.Name);
                    var delta = template.QueueSizeToMaintain - deployedTemplates;
                    SimpleTrace.TraceInformation($"Template {template.Name} has {deployedTemplates} RGs deployed and requires {template.QueueSizeToMaintain}");

                    for (int i = 1; i <= delta; i++)
                    {
                        SimpleTrace.TraceInformation($"Template {template.Name} creating {i} of {delta}");
                        CreateResourceGroupOperation(template.Config.Subscriptions.OrderBy(a => Guid.NewGuid()).First(), template.Config.Regions.OrderBy(a => Guid.NewGuid()).First(), template.Name);
                        createdMissingTemplates++;
                    }
                    for (int i = 1; i <= -delta; i++)
                    {
                        var resourceGroup= await StorageHelper.GetQueueMessage(template.QueueName);
                        if (resourceGroup!=null)
                        {
                            SimpleTrace.TraceInformation($"Template {template.Name} deleting {i} of {-delta}->{resourceGroup.CsmId}");
                            //RemoveFromFreeResourcesQueue(resourceGroup);
                            DeleteResourceGroupOperation(resourceGroup);
                        }
                    }
                }
                AppInsights.TelemetryClient.TrackMetric("createdMissingTemplates", createdMissingTemplates);
                AppInsights.TelemetryClient.TrackMetric("createdMissingRGs", createdMissingRGs);
                AppInsights.TelemetryClient.TrackMetric("deletedDuplicateRGs", deletedDuplicateRGs);
                AppInsights.TelemetryClient.TrackMetric("fullCleanupTime", sw.Elapsed.TotalSeconds);
                sw.Stop();
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, "CleanupSubscriptions error, Count {Count}", Interlocked.Increment(ref _maintainResourceGroupListErrorCount));
            }
        }

        //private void RemoveFromFreeResourcesQueue(ResourceGroup resourceGroup)
        //{
        //    SimpleTrace.TraceInformation($"Removing {resourceGroup.CsmId} from FreeResourceQueue");
        //    if (StorageHelper.PeekQueueMessages(resourceGroup.QueueName).GetAwaiter().GetResult().ToList().Any(r => string.Equals(r.CsmId, resourceGroup.CsmId, StringComparison.OrdinalIgnoreCase)))
        //    {
        //        SimpleTrace.TraceInformation($"Didnt find {resourceGroup.CsmId} in FreeResourceQueue. Dequeueing");
        //        var dequeueCount = StorageHelper.GetQueueCount(resourceGroup.QueueName).ConfigureAwait(false).GetAwaiter().GetResult();
        //        SimpleTrace.TraceInformation($"Searching for {resourceGroup.CsmId} in FreeResourceQueue for max dequeueCount: {dequeueCount}");

        //        ResourceGroup temp = StorageHelper.GetQueueMessage(resourceGroup.QueueName).ConfigureAwait(false).GetAwaiter().GetResult();

        //        while (dequeueCount-- >= 0 && temp != null)
        //        {
        //            SimpleTrace.TraceInformation($"Attempt {dequeueCount} looking for {resourceGroup.CsmId} in FreeResourceQueue. Found {temp.CsmId}");

        //            if (string.Equals(temp.ResourceGroupName, resourceGroup.ResourceGroupName, StringComparison.OrdinalIgnoreCase))
        //            {
        //                SimpleTrace.TraceInformation($"Searching for {resourceGroup.CsmId} in FreeResourceQueue. Matched {temp.CsmId} . Returning");
        //                return;
        //            }
        //            else
        //            {
        //                SimpleTrace.TraceInformation($"Searching for {resourceGroup.CsmId} in FreeResourceQueue. Didnt match {temp.CsmId} . Enqueueing");
        //                StorageHelper[resourceGroup.TemplateName].Enqueue(temp);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        SimpleTrace.TraceInformation($"Didnt find {resourceGroup.CsmId} in FreeResourceQueue");
        //    }
        //}

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
            //var freeSitesCount = FreeResourceGroups.Values.SelectMany(e => e).ToList().Count(sub => sub.SubscriptionType == SubscriptionType.AppService);
            //var inUseSites = ResourceGroupsInUse.Select(s => s.Value).Where(sub => sub.SubscriptionType == SubscriptionType.AppService);
            var resourceGroups = StorageHelper.GetAllFreeResources().ConfigureAwait(false).GetAwaiter().GetResult();
            var inUseSitesCount = resourceGroups.Count();
            var inUseFunctionsCount = resourceGroups.Count(res => res.AppService == AppService.Function);
            var inUseWebsitesCount = resourceGroups.Count(res => res.AppService == AppService.Web);
            var inUseContainerCount = resourceGroups.Count(res => res.AppService == AppService.Containers);
            var inUseApiAppCount = resourceGroups.Count(res => res.AppService == AppService.Api);
            var inProgress = ResourceGroupsInProgress.Select(s => s.Value).Count();
            var backgroundOperations = BackgroundInternalOperations.Select(s => s.Value).Count();
            //var freeLinuxResources = FreeResourceGroups.Values.SelectMany(e => e).ToList().Count(sub => sub.SubscriptionType == SubscriptionType.Linux);
            //var freeVSCodeLinuxResources = FreeResourceGroups.Values.SelectMany(e => e).ToList().Count(sub => sub.SubscriptionType == SubscriptionType.VSCodeLinux);

            //var inUseLinuxResources = ResourceGroupsInUse.Select(s => s.Value).Count(sub => sub.SubscriptionType == SubscriptionType.Linux);
            //var inUseVSCodeLinuxResources = ResourceGroupsInUse.Select(s => s.Value).Count(sub => sub.SubscriptionType == SubscriptionType.VSCodeLinux);

            //AppInsights.TelemetryClient.TrackMetric("freeSites", freeSitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseSites", inUseSitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseFunctionsCount", inUseFunctionsCount);
            AppInsights.TelemetryClient.TrackMetric("inUseWebsitesCount", inUseWebsitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseContainersCount", inUseContainerCount);
            AppInsights.TelemetryClient.TrackMetric("inUseApiAppCount", inUseApiAppCount);
            AppInsights.TelemetryClient.TrackMetric("inUseSites", inUseSitesCount);

            AppInsights.TelemetryClient.TrackMetric("inProgressOperations", inProgress);
            AppInsights.TelemetryClient.TrackMetric("backgroundOperations", backgroundOperations);
            //AppInsights.TelemetryClient.TrackMetric("freeLinuxResources", freeLinuxResources);
            //AppInsights.TelemetryClient.TrackMetric("inUseLinuxResources", inUseLinuxResources);
            //AppInsights.TelemetryClient.TrackMetric("freeVSCodeLinuxResources", freeVSCodeLinuxResources);
            //AppInsights.TelemetryClient.TrackMetric("inUseVSCodeLinuxResources", inUseVSCodeLinuxResources);
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

        private void CreateResourceGroupOperation(string subscriptionId, string geoRegion, string templateName = "")
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Creating resourceGroup in {subscriptionId} in {geoRegion} with templateName {templateName}",
                Type = OperationType.ResourceGroupCreate,
                Task = CsmManager.CreateResourceGroup(subscriptionId, geoRegion, templateName),
                RetryAction = () => CreateResourceGroupOperation(subscriptionId, geoRegion, templateName)
            });
        }

        private void PutResourceGroupInDesiredStateOperation(ResourceGroup resourceGroup)
        {
            AddOperation(new BackgroundOperation<ResourceGroup>
            {
                Description = $"Putting resourceGroup {resourceGroup.CsmId} in desired state {resourceGroup.AppService} {resourceGroup.TemplateName}",
                Type = OperationType.ResourceGroupPutInDesiredState,
                Task = resourceGroup.PutInDesiredState()
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