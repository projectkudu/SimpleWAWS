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

        public readonly ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>> FreeResourceGroups = new ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>>();

        public IEnumerable<ResourceGroup> LoadedResourceGroups
        {
            get
            {
                return FreeResourceGroups.Values.SelectMany(e => e).ToList()
                       .Concat(ResourceGroupsInUse.Where(e => e.Value != null).Select(e => e.Value))
                       .Concat(ResourceGroupsInProgress.Where(e => e.Value != null).Select(e => e.Value.ResourceGroup));
            }
        }
        //public readonly ConcurrentQueue<ResourceGroup> FreeVSCodeLinuxResourceGroups = new ConcurrentQueue<ResourceGroup>();
        //public ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>> FreeVSCodeLinuxResourceGroups = new ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>>();
        //public readonly ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>> FreeLinuxResourceGroups = new ConcurrentDictionary<string, ConcurrentQueue<ResourceGroup>>();
        public readonly ConcurrentDictionary<string, InProgressOperation> ResourceGroupsInProgress = new ConcurrentDictionary<string, InProgressOperation>();
        public readonly ConcurrentDictionary<string, ResourceGroup> ResourceGroupsInUse = new ConcurrentDictionary<string, ResourceGroup>();
        public readonly ConcurrentDictionary<Guid, BackgroundOperation> BackgroundInternalOperations = new ConcurrentDictionary<Guid, BackgroundOperation>();
        private Timer _logQueueStatsTimer;
        private Timer _cleanupSubscriptionsTimer;
        private Timer _cleanupExpiredResourceGroupsTimer;
        private readonly JobHost _jobHost = new JobHost();
        private static int _maintainResourceGroupListErrorCount = 0;
        private static Random rand = new Random();
        internal Stopwatch _uptime;
        internal int _cleanupOperationsTriggered;
        public ResourceGroup MonitoringResourceGroup;
        public static ConcurrentDictionary<string, DateTime> MonitoringResourceGroupCheckoutTimes = new ConcurrentDictionary<string, DateTime>();

        public BackgroundQueueManager()
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

        public void DeleteResourceGroup(ResourceGroup resourceGroup)
        {
            ResourceGroup temp;
            if (this.ResourceGroupsInUse.TryRemove(resourceGroup.UserId, out temp))
            {
                SimpleTrace.TraceInformation("{0} removed for user {1}", temp.CsmId, resourceGroup.UserId);
            }
            else
            {
                SimpleTrace.TraceError("No resourcegroup checked out for {0}", temp.UserId);
            }
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
                    var result = subscription.GetSubscriptionStats();
                    foreach (var resourceGroup in result.Ready)
                    {
                        PutResourceGroupInDesiredStateOperation(resourceGroup);
                    }
                    //if (result.ToCreateTemplates != null)
                    //{
                    //    var rand = new Random();
                    //    foreach (var template in result.ToCreateTemplates)
                    //    {
                    //        for (int i = 0; i < template.RemainingCount; i++)
                    //        {
                    //            CreateResourceGroupOperation(subscription.SubscriptionId, subscription.GeoRegions.ElementAt(rand.Next(subscription.GeoRegions.Count())), template.TemplateName);
                    //        }
                    //    }
                    //}
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
                        if (!string.IsNullOrEmpty(readyToAddRg.DeployedTemplateName) && !string.IsNullOrEmpty(readyToAddRg.SiteGuid))
                        {
                            if (!FreeResourceGroups.ContainsKey(readyToAddRg.DeployedTemplateName))
                            {
                                readyToAddRg.TemplateName = readyToAddRg.DeployedTemplateName;
                                FreeResourceGroups.GetOrAdd(readyToAddRg.DeployedTemplateName, new ConcurrentQueue<ResourceGroup>());
                            }
                            readyToAddRg.TemplateName = readyToAddRg.DeployedTemplateName;
                            FreeResourceGroups[readyToAddRg.DeployedTemplateName].Enqueue(readyToAddRg);
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
                _jobHost.DoWork(() =>
                {
                    var resources = this.ResourceGroupsInUse
                        .Select(e => e.Value)
                        .Where(rg => (int)rg.TimeLeft.TotalSeconds == 0);

                    foreach (var resource in resources)
                    {
                        DeleteResourceGroup(resource);
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
                    sub.ResourceGroups = LoadedResourceGroups.Where(r => r.SubscriptionId == sub.SubscriptionId);
                    var trialsubresult = sub.GetSubscriptionStats();
                    toDelete.AddRange(trialsubresult.ToDelete);
                    ready.AddRange(trialsubresult.Ready);
                }

                var rand = new Random();
                foreach (var resourceGroup in toDelete)
                {
                    RemoveFromFreeResourcesQueue(resourceGroup);
                    DeleteResourceGroupOperation(resourceGroup);
                    deletedDuplicateRGs++;
                }
                foreach (var template in TemplatesManager.GetTemplates())
                {
                    var deployedTemplates = ready.Count(a => a.DeployedTemplateName == template.Name);
                    for (int i = 1; i <= template.QueueSizeToMaintain - deployedTemplates; i++)
                    {
                        CreateResourceGroupOperation(template.Config.Subscriptions.OrderBy(a => Guid.NewGuid()).First(), template.Config.Regions.OrderBy(a => Guid.NewGuid()).First(), template.Name);
                        createdMissingTemplates++;
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

        private void RemoveFromFreeResourcesQueue(ResourceGroup resourceGroup)
        {
            SimpleTrace.TraceInformation($"Removing {resourceGroup.CsmId} from FreeResourceQueue");
            if (this.FreeResourceGroups[resourceGroup.TemplateName].ToList().Any(r => string.Equals(r.CsmId, resourceGroup.CsmId, StringComparison.OrdinalIgnoreCase)))
            {
                SimpleTrace.TraceInformation($"Didnt find {resourceGroup.CsmId} in FreeResourceQueue. Dequeueing");
                var dequeueCount = this.FreeResourceGroups[resourceGroup.TemplateName].Count;
                SimpleTrace.TraceInformation($"Searching for {resourceGroup.CsmId} in FreeResourceQueue for max dequeueCount: {dequeueCount}");

                ResourceGroup temp;
                
                while (dequeueCount-- >= 0 && (this.FreeResourceGroups[resourceGroup.TemplateName].TryDequeue(out temp)))
                {
                    SimpleTrace.TraceInformation($"Attempt {dequeueCount} looking for {resourceGroup.CsmId} in FreeResourceQueue. Found {temp.CsmId}");

                    if (string.Equals(temp.ResourceGroupName, resourceGroup.ResourceGroupName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        SimpleTrace.TraceInformation($"Searching for {resourceGroup.CsmId} in FreeResourceQueue. Matched {temp.CsmId} . Returning");
                        return;
                    }
                    else
                    {
                        SimpleTrace.TraceInformation($"Searching for {resourceGroup.CsmId} in FreeResourceQueue. Didnt match {temp.CsmId} . Enqueueing");
                        this.FreeResourceGroups[resourceGroup.TemplateName].Enqueue(temp);
                    }
                }
            }
            else
            {
                SimpleTrace.TraceInformation($"Didnt find {resourceGroup.CsmId} in FreeResourceQueue");
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
            var freeSitesCount = FreeResourceGroups.Values.SelectMany(e => e).ToList().Count(sub => sub.SubscriptionType == SubscriptionType.AppService);
            var inUseSites = ResourceGroupsInUse.Select(s => s.Value).Where(sub => sub.SubscriptionType == SubscriptionType.AppService);
            var resourceGroups = inUseSites as IList<ResourceGroup> ?? inUseSites.ToList();
            var inUseSitesCount = resourceGroups.Count();
            var inUseFunctionsCount = resourceGroups.Count(res => res.AppService == AppService.Function);
            var inUseWebsitesCount = resourceGroups.Count(res => res.AppService == AppService.Web);
            var inUseContainerCount = resourceGroups.Count(res => res.AppService == AppService.Containers);
            var inUseApiAppCount = resourceGroups.Count(res => res.AppService == AppService.Api);
            var inProgress = ResourceGroupsInProgress.Select(s => s.Value).Count();
            var backgroundOperations = BackgroundInternalOperations.Select(s => s.Value).Count();
            var freeLinuxResources = FreeResourceGroups.Values.SelectMany(e => e).ToList().Count(sub => sub.SubscriptionType == SubscriptionType.Linux);
            var freeVSCodeLinuxResources = FreeResourceGroups.Values.SelectMany(e => e).ToList().Count(sub => sub.SubscriptionType == SubscriptionType.VSCodeLinux);

            var inUseLinuxResources = ResourceGroupsInUse.Select(s => s.Value).Count(sub => sub.SubscriptionType == SubscriptionType.Linux);
            var inUseVSCodeLinuxResources = ResourceGroupsInUse.Select(s => s.Value).Count(sub => sub.SubscriptionType == SubscriptionType.VSCodeLinux);

            AppInsights.TelemetryClient.TrackMetric("freeSites", freeSitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseSites", inUseSitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseFunctionsCount", inUseFunctionsCount);
            AppInsights.TelemetryClient.TrackMetric("inUseWebsitesCount", inUseWebsitesCount);
            AppInsights.TelemetryClient.TrackMetric("inUseContainersCount", inUseContainerCount);
            AppInsights.TelemetryClient.TrackMetric("inUseApiAppCount", inUseApiAppCount);
            AppInsights.TelemetryClient.TrackMetric("inUseSites", inUseSitesCount);

            AppInsights.TelemetryClient.TrackMetric("inProgressOperations", inProgress);
            AppInsights.TelemetryClient.TrackMetric("backgroundOperations", backgroundOperations);
            AppInsights.TelemetryClient.TrackMetric("freeLinuxResources", freeLinuxResources);
            AppInsights.TelemetryClient.TrackMetric("inUseLinuxResources", inUseLinuxResources);
            AppInsights.TelemetryClient.TrackMetric("freeVSCodeLinuxResources", freeVSCodeLinuxResources);
            AppInsights.TelemetryClient.TrackMetric("inUseVSCodeLinuxResources", inUseVSCodeLinuxResources);
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