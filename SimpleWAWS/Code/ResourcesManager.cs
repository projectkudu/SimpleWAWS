using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Kudu.Client.Editor;
using Kudu.Client.Zip;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.WebSites;
using Newtonsoft.Json;
using SimpleWAWS.Authentication;
using System.Security.Principal;
using ARMClient.Library;
using SimpleWAWS.Code;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Models.CsmModels;

namespace SimpleWAWS.Models
{
    public class ResourcesManager
    {
        public static TimeSpan ResourceGroupExpiryTime;

        private readonly ConcurrentQueue<ResourceGroup> _freeResourceGroups = new ConcurrentQueue<ResourceGroup>();
        private readonly ConcurrentDictionary<string, Task> _resourceGroupsInProgress = new ConcurrentDictionary<string, Task>();
        private readonly ConcurrentDictionary<string, ResourceGroup> _resourceGroupsInUse = new ConcurrentDictionary<string, ResourceGroup>();

        private static readonly AsyncLock _lock = new AsyncLock(); 
        private Timer _timer;
        private int _logCounter = 0;
        private readonly JobHost _jobHost = new JobHost();

        private static ResourcesManager _instance;
        public static async Task<ResourcesManager> GetInstanceAsync()
        {
            //avoid the async lock for normal case
            if (_instance != null)
            {
                return _instance;
            }

            using (await _lock.LockAsync())
            {
                if (_instance == null)
                {
                    _instance = new ResourcesManager();
                    await _instance.LoadAzureResources();
                }
            }

            return _instance;
        }

        private ResourcesManager()
        {
            ResourceGroupExpiryTime = TimeSpan.FromMinutes(Int32.Parse(ConfigurationManager.AppSettings["siteExpiryMinutes"]));
        }

        // ARM
        private async Task LoadAzureResources()
        {
            // Load all subscriptions
            var subscriptions = (await Task.WhenAll(ConfigurationManager.AppSettings["subscriptions"].Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Util.SafeGuard(() => new Subscription(s).Load())))).Where(s => s != null);

            //Create Trial resources if they are not already created
            await Task.WhenAll(subscriptions.Select(s => s.MakeTrialSubscription()));

            // Check if the sites are in use and place them in the right list
            var tasksList = new List<Task<ResourceGroup>>();
            foreach (var resourceGroup in subscriptions.Select(s => s.ResourceGroups).SelectMany(r => r))
            {
                if (resourceGroup.UserId != null)
                {
                    Trace.TraceInformation("Loading ResourceGroup {0} into the InUse list", resourceGroup.ResourceGroupName);
                    if (!_resourceGroupsInUse.TryAdd(resourceGroup.UserId, resourceGroup))
                    {
                        Trace.TraceError("user {0} already had a resourceGroup in the dictionary extra resourceGroup is {1}. This shouldn't happen. Deleting and replacing the ResourceGroup.", resourceGroup.UserId, resourceGroup.ResourceGroupName);
                        tasksList.Add(resourceGroup.DeleteAndCreateReplacement());
                    }
                }
                else
                {
                    Trace.TraceInformation("Loading site {0} into the Free list", resourceGroup.ResourceGroupName);
                    _freeResourceGroups.Enqueue(resourceGroup);
                }
            }

            var newResourceGroups = await Task.WhenAll(tasksList);

            foreach (var resourceGroup in newResourceGroups)
            {
                _freeResourceGroups.Enqueue(resourceGroup);
            }

            // Do maintenance on the resourceGroup lists every minute (and start one right now)
            if (_timer == null)
            {
                _timer = new Timer(OnTimerElapsed);
                _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(60 * 1000));
            }
        }

        // ARM
        private async Task MaintainResourceGroupLists()
        {
            await DeleteExpiredResourceGroupsAsync();
        }

        // ARM
        private void OnTimerElapsed(object state)
        {
            try
            {
                _jobHost.DoWork(() =>
                {
                    MaintainResourceGroupLists().Wait();
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
                Trace.TraceError(e.ToString());
            }
        }

        // ARM
        private async Task LogActiveUsageStatistics(ResourceGroup resourceGroup)
        {
            var site = resourceGroup.Sites.First();
            var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
            var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials);
            using (var httpContentStream = await zipManager.GetZipFileStreamAsync("LogFiles/http/RawLogs"))
            {
                await StorageHelper.UploadBlob(resourceGroup.ResourceUniqueId, httpContentStream);
            }
            await StorageHelper.AddQueueMessage(new {BlobName = resourceGroup.ResourceUniqueId});
        }

        // ARM
        private async Task DeleteExpiredResourceGroupsAsync()
        {
            await Task.WhenAll(
                this._resourceGroupsInUse
                .Select(e => e.Value)
                .Where(rg => DateTime.UtcNow - rg.StartTime > ResourceGroupExpiryTime)
                .Select(DeleteResourceGroup)
                );
        }

        // ARM
        private async Task DeleteResourceGroup(ResourceGroup resourceGroup)
        {
            Trace.TraceInformation("Deleting expired resourceGroup {0}", resourceGroup.ResourceGroupName);
            await LogActiveUsageStatistics(resourceGroup);
            ResourceGroup temp;
            this._resourceGroupsInUse.TryRemove(resourceGroup.UserId, out temp);
            var newResourceGroup =  await resourceGroup.DeleteAndCreateReplacement();
            this._freeResourceGroups.Enqueue(newResourceGroup);
        }

        // ARM
        private async Task<ResourceGroup> ActivateResourceGroup(TryWebsitesIdentity userIdentity, AppService appService, Func<ResourceGroup, Task<ResourceGroup>> func)
        {
            ResourceGroup resourceGroup = null;
            if (userIdentity == null)
            {
                throw new InvalidUserIdentityException("userIdentity was empty");
            }

            var userId = userIdentity.Name;
            var tokenSource = new CancellationTokenSource();
            try
            {
                if (_resourceGroupsInUse.TryGetValue(userId, out resourceGroup))
                {
                    throw new MoreThanOneResourceGroupException("You can't have more than 1 free resource at a time");
                }

                if (_freeResourceGroups.TryDequeue(out resourceGroup))
                {
                    //mark site in use as soon as it's checked out so that if there is a reload it will be sorted out to the used queue.
                    await resourceGroup.MarkInUse(userId, ResourceGroupExpiryTime, appService);
                    var rbacTask = Task.FromResult(false); //RbacHelper.AddRbacUser(userIdentity.Puid, userIdentity.Email, resourceGroup);

                    var resourceGroupCreationTask = Task.Delay(Timeout.Infinite, tokenSource.Token);
                    // This should not be awaited. this is retuning the infinite task from above
                    // The purpose of this task is to block the GetResourceGroupAsync() call until the resourceGroup in progress is done.
                    // Otherwise the if the user refreshes, they will be offered to create another resource eventhough there is one already in progress for them.
                    // When the resourceGroup is no longer in progress, we cancel the task using the cancellation token.
                    _resourceGroupsInProgress.AddOrUpdate(userId, s => resourceGroupCreationTask, (s, task) => resourceGroupCreationTask).Ignore();
                    Trace.TraceInformation("resourceGroup {0} is now in use", resourceGroup.ResourceGroupName);

                    resourceGroup = await func(resourceGroup);

                    var addedResourceGroup = _resourceGroupsInUse.GetOrAdd(userId, resourceGroup);
                    if (addedResourceGroup.ResourceGroupName == resourceGroup.ResourceGroupName)
                    {
                        //this means we just added the resourceGroup for the user.
                        await addedResourceGroup.MarkInUse(userId, ResourceGroupExpiryTime, appService);
                        return addedResourceGroup;
                    }
                    else
                    {
                        //this means the user is trying to add more than 1 site.
                        //delete the new site that's not yet added to the used list
                        await resourceGroup.DeleteAndCreateReplacement();
                        throw new MoreThanOneResourceGroupException("You can't have more than 1 free resource at a time");
                    }
                }
                else
                {
                    throw new NoFreeResourceGroupsException("No free resources are available currently. Please try again later.");
                }
                // End site specific stuff
            }
            catch (MoreThanOneResourceGroupException)
            {
                throw;
            }
            catch (NoFreeResourceGroupsException)
            {
                throw;
            }
            catch (Exception e)
            {
                //unknown exception, log it
                Trace.TraceError(e.ToString());
            }
            finally
            {
                Task temp;
                _resourceGroupsInProgress.TryRemove(userId, out temp);
                tokenSource.Cancel();
                LogQueueStatistics();
            }
            //if we are here that means a bad exception happened above, but we might leak a site if we don't remove the site and replace it correctly.
            if (resourceGroup != null)
            {
                //no need to await this call
                //this call is to fix our internal state, return an error right away to the caller
                ThreadPool.QueueUserWorkItem(async o => await DeleteResourceGroup(resourceGroup));
            }
            throw new Exception("An Error occured. Please try again later.");
        }

        // ARM
        public async Task<ResourceGroup> ActivateWebApp(WebsiteTemplate template, TryWebsitesIdentity userIdentity, AppService temp = AppService.Web)
        {
            // Start site specific stuff
            return await ActivateResourceGroup(userIdentity, temp, async resourceGroup =>
                {
                    Trace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
                            template.Language, template.Name, resourceGroup.ResourceUniqueId);

                    var site = resourceGroup.Sites.First();
                    var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                    if (template != null && template.FileName != null)
                    {
                        var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                        var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials);
                        Task zipUpload = zipManager.PutZipFileAsync("site/wwwroot", template.GetFullPath());
                        var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials);
                        Task deleteHostingStart = vfsManager.Delete("site/wwwroot/hostingstart.html");
                        await Task.WhenAll(zipUpload, deleteHostingStart);
                    }
                    site.FireAndForget();
                    resourceGroup.IsRbacEnabled = await rbacTask;
                    return resourceGroup;
                });
        }

        // ARM
        public async Task<ResourceGroup> ActivateMobileApp(WebsiteTemplate template, TryWebsitesIdentity userIdentity)
        {
            return await ActivateWebApp(template, userIdentity, AppService.Mobile);
        }

        // ARM
        public async Task<ResourceGroup> ActivateApiApp(ApiTemplate template, TryWebsitesIdentity userIdentity)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Api, async resourceGroup =>
            {

                Trace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
                            "Api", template.ApiTemplateName, resourceGroup.ResourceUniqueId);

                var apiApp = new ApiApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, Guid.NewGuid().ToString().Replace("-", ""))
                {
                    MicroserviceId = template.ApiTemplateName,
                    Location = resourceGroup.GeoRegion
                };

                var csmTemplate = await apiApp.GenerateCsmTemplate();

                var templateWrapper = new CsmTemplateWrapper
                {
                    properties = new CsmTemplateProperties
                    {
                        mode = "Incremental",
                        parameters = apiApp.GenerateTemplateParameters(),
                        template = csmTemplate
                    }
                };

                var deployment = new CsmDeployment
                {
                    DeploymentName = resourceGroup.ResourceUniqueId,
                    SubscriptionId = resourceGroup.SubscriptionId,
                    ResourceGroupName = resourceGroup.ResourceGroupName,
                    CsmTemplate = templateWrapper
                };

                await deployment.Deploy(block: true);

                // After a deployment, we have no idea what changes happened in the resource group
                // we should reload it.
                // TODO: consider reloading the resourceGroup along with the deployment itself.
                await resourceGroup.Load();

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                var publicAccessTask = resourceGroup.ApiApps.Select(a => a.SetAccessLevel("PublicAnonymous"));
                resourceGroup.IsRbacEnabled = await rbacTask;
                await Task.WhenAll(publicAccessTask);
                return resourceGroup;
            });
        }

        // ARM
        public async Task<ResourceGroup> ActivateLogicApp(LogicTemplate template, TryWebsitesIdentity userIdentity)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Logic, resourceGroup => { return Task.FromResult(resourceGroup); });
        }

        // ARM
        public async Task<ResourceGroup> GetResourceGroup(string userId)
        {
            ResourceGroup resourceGroup;
            _resourceGroupsInUse.TryGetValue(userId, out resourceGroup);
            if (resourceGroup == null)
            {
                Task temp;
                if (_resourceGroupsInProgress.TryGetValue(userId, out temp))
                {
                    try
                    {
                        await temp;
                    }
                    catch (TaskCanceledException)
                    {
                        //expected
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError(e.ToString());
                    }
                    _resourceGroupsInUse.TryGetValue(userId, out resourceGroup);
                }
            }
            return resourceGroup;
        }

        // ARM
        public async Task ResetAllFreeResourceGroups()
        {
            using (await _lock.LockAsync())
            {
                var list = new List<ResourceGroup>();
                while (!_freeResourceGroups.IsEmpty)
                {
                    ResourceGroup temp;
                    if (_freeResourceGroups.TryDequeue(out temp))
                    {
                        list.Add(temp);
                    }
                }
                await Task.WhenAll(list.Select(resourceGroup =>
                {
                    Trace.TraceInformation("Deleting site {0}", resourceGroup.ResourceGroupName);
                    return DeleteResourceGroup(resourceGroup);
                }));
            }
        }

        // ARM
        public async Task DropAndReloadFromAzure()
        {
            using (await _lock.LockAsync())
            {
                while (!_freeResourceGroups.IsEmpty)
                {
                    ResourceGroup temp;
                    _freeResourceGroups.TryDequeue(out temp);
                }
                _resourceGroupsInUse.Clear();
                await LoadAzureResources();
            }
        }

        // ARM
        public async Task DeleteResourceGroup(string userId)
        {
            ResourceGroup resourceGroup;
            _resourceGroupsInUse.TryGetValue(userId, out resourceGroup);

            if (resourceGroup != null)
            {
                await DeleteResourceGroup(resourceGroup);
            }
        }

        public IReadOnlyCollection<ResourceGroup> GetAllFreeResourceGroups()
        {
            return _freeResourceGroups.ToList();
        }

        // ARM
        public IReadOnlyCollection<ResourceGroup> GetAllInUseResourceGroups()
        {
            return _resourceGroupsInUse.ToList().Select(s => s.Value).ToList();
        }

        public int GetAllInProgressResourceGroupsCount()
        {
            return this._resourceGroupsInProgress.Count;
        }

        private void LogQueueStatistics()
        {
        }
    }

}
