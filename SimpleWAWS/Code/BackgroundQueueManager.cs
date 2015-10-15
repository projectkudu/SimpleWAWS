using Kudu.Client.Zip;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code
{
    public class BackgroundQueueManager
    {

        public readonly ConcurrentQueue<ResourceGroup> FreeResourceGroups = new ConcurrentQueue<ResourceGroup>();
        public readonly ConcurrentDictionary<string, InProgressOperation> ResourceGroupsInProgress = new ConcurrentDictionary<string, InProgressOperation>();
        public readonly ConcurrentDictionary<string, ResourceGroup> ResourceGroupsInUse = new ConcurrentDictionary<string, ResourceGroup>();
        public readonly ConcurrentDictionary<Guid, BackgroundOperation> BackgroundInternalOperations = new ConcurrentDictionary<Guid, BackgroundOperation>();
        private Timer _timer;
        private readonly JobHost _jobHost = new JobHost();
        private int _logCounter = 0;
        private static int _maintainResourceGroupListErrorCount = 0;

        public BackgroundQueueManager()
        {
            if (_timer == null)
            {
                _timer = new Timer(OnTimerElapsed);
                _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(60 * 1000));
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
                LogUsageStatistics(resourceGroup);
            }
        }

        private async Task<ResourceGroup> LogActiveUsageStatistics(ResourceGroup resourceGroup)
        {
            try
            {
                var site = resourceGroup.Sites.First(s => s.IsSimpleWAWSOriginalSite);
                var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials);
                using (var httpContentStream = await zipManager.GetZipFileStreamAsync("LogFiles/http/RawLogs"))
                {
                    await StorageHelper.UploadBlob(resourceGroup.ResourceUniqueId, httpContentStream);
                }
                await StorageHelper.AddQueueMessage(new { BlobName = resourceGroup.ResourceUniqueId });
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
                    foreach(var geoRegion in result.ToCreateInRegions)
                    {
                        CreateResourceGroupOperation(subscription.SubscriptionId, geoRegion);
                    }
                    foreach(var resourceGroup in result.ToDelete)
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
                        FreeResourceGroups.Enqueue(readyToAddRg);
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

        private void OnTimerElapsed(object state)
        {
            try
            {
                _jobHost.DoWork(() =>
                {
                    MaintainResourceGroupLists();
                    _logCounter++;
                    if (_logCounter % 5 == 0)
                    {
                        //log only every 5 minutes
                        LogQueueStatistics();
                        _logCounter = 0;
                    }
                });
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, "MainTainResourceGroupLists error, Count {Count}", Interlocked.Increment(ref _maintainResourceGroupListErrorCount));
            }
        }

        private void MaintainResourceGroupLists()
        {
            var resources = this.ResourceGroupsInUse
                .Select(e => e.Value)
                .Where(rg => (int)rg.TimeLeft.TotalSeconds == 0);

            foreach (var resource in resources)
                DeleteResourceGroup(resource);
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
                Task = resourceGroup.DeleteAndCreateReplacement(),
                RetryAction = () => DeleteAndCreateResourceGroupOperation(resourceGroup)
            });
        }

        private void LogQueueStatistics()
        {
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
            BackgroundInternalOperations.TryAdd(operation.OperationId, operation);
            operation.Task.ContinueWith(_ => HandleBackgroundOperation(operation)).ConfigureAwait(false);
        }
    }
}