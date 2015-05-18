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
using Newtonsoft.Json.Linq;
using SimpleWAWS.Trace;
using System.Web.Hosting;

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

        private static int _stateInconsistencyErrorCount = 0;
        private static int _maintainResourceGroupListErrorCount = 0;
        private static int _unknownErrorInCreateErrorCount = 0;
        private static int _getResourceGroupErrorCount = 0;
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
            ResourceGroupExpiryTime = TimeSpan.FromMinutes(Int32.Parse(SimpleSettings.SiteExpiryMinutes));
        }

        // ARM
        private async Task LoadAzureResources()
        {
            // Load all subscriptions
            var subscriptions = await SimpleSettings.Subscriptions.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => new Subscription(s).Load())
                .IgnoreAndFilterFailures();

            //Create Trial resources if they are not already created
            subscriptions = await subscriptions.Select(s => s.MakeTrialSubscription()).IgnoreAndFilterFailures();

            // Check if the sites are in use and place them in the right list
            var tasksList = new List<Task<ResourceGroup>>();
            foreach (var resourceGroup in subscriptions.Select(s => s.ResourceGroups).SelectMany(r => r))
            {
                if (resourceGroup.UserId != null)
                {
                    SimpleTrace.Diagnostics.Verbose("Loading ResourceGroup {resourceGroupId} into the InUse list", resourceGroup.CsmId);
                    if (!_resourceGroupsInUse.TryAdd(resourceGroup.UserId, resourceGroup))
                    {
                        SimpleTrace.Diagnostics.Fatal("user {user} already had a resourceGroup in the dictionary extra resourceGroup is {resourceGroupId}. This shouldn't happen. Deleting and replacing the ResourceGroup. Count {Count}", resourceGroup.UserId, resourceGroup.CsmId, Interlocked.Increment(ref _stateInconsistencyErrorCount));
                        tasksList.Add(resourceGroup.DeleteAndCreateReplacement());
                    }
                }
                else
                {
                    SimpleTrace.Diagnostics.Verbose("Loading resourceGroup {resourceGroupId} into the Free list", resourceGroup.CsmId);
                    _freeResourceGroups.Enqueue(resourceGroup);
                }
            }

            var newResourceGroups = await tasksList.WhenAll();

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
                SimpleTrace.Diagnostics.Fatal(e, "MainTainResourceGroupLists error, Count {Count}", Interlocked.Increment(ref _maintainResourceGroupListErrorCount));
            }
        }

        // ARM
        private async Task LogActiveUsageStatistics(ResourceGroup resourceGroup)
        {
            try
            {


                var site = resourceGroup.Sites.First();
                var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials);
                using (var httpContentStream = await zipManager.GetZipFileStreamAsync("LogFiles/http/RawLogs"))
                {
                    await StorageHelper.UploadBlob(resourceGroup.ResourceUniqueId, httpContentStream);
                }
                await StorageHelper.AddQueueMessage(new { BlobName = resourceGroup.ResourceUniqueId });
            }
            catch { }
        }

        // ARM
        private async Task DeleteExpiredResourceGroupsAsync()
        {
            await this._resourceGroupsInUse
                .Select(e => e.Value)
                .Where(rg => DateTime.UtcNow - rg.StartTime > ResourceGroupExpiryTime)
                .Select(DeleteResourceGroup)
                .WhenAll();
        }

        // ARM
        private async Task DeleteResourceGroup(ResourceGroup resourceGroup)
        {
            SimpleTrace.Diagnostics.Information("Deleting expired resourceGroup {resourceGroupId}", resourceGroup.CsmId);
            if (resourceGroup.AppService == AppService.Web)
            {
                await LogActiveUsageStatistics(resourceGroup);
            }
            ResourceGroup temp;
            this._resourceGroupsInUse.TryRemove(resourceGroup.UserId, out temp);
            HostingEnvironment.QueueBackgroundWorkItem(async (c) => { 
                var newResourceGroup = await resourceGroup.DeleteAndCreateReplacement().ConfigureAwait(false);
                this._freeResourceGroups.Enqueue(newResourceGroup);
            });
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
                    SimpleTrace.Diagnostics.Information("resourceGroup {resourceGroupId} is now in use", resourceGroup.CsmId);

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
                SimpleTrace.Diagnostics.Fatal(e, "Unknown error during UserCreate, Count {Count}", Interlocked.Increment(ref _unknownErrorInCreateErrorCount));
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
                    SimpleTrace.Analytics.Information(AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName,
                        userIdentity, template, resourceGroup.CsmId);
                    SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
                            template.Language, template.Name, resourceGroup.ResourceUniqueId);

                    var site = resourceGroup.Sites.First();
                    var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                    if (template != null && template.FileName != null)
                    {
                        var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                        var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials, retryCount: 3);
                        Task zipUpload = zipManager.PutZipFileAsync("site/wwwroot", template.GetFullPath());
                        var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);
                        Task deleteHostingStart = vfsManager.Delete("site/wwwroot/hostingstart.html");
                        await Task.WhenAll(zipUpload, deleteHostingStart);
                    }
                    site.AppSettings["LAST_MODIFIED_TIME_UTC"] = DateTime.UtcNow.ToString();
                    site.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = SimpleSettings.SiteExpiryMinutes;
                    site.AppSettings["MONACO_EXTENSION_VERSION"] = "beta";
                    site.AppSettings["WEBSITE_TRY_MODE"] = "1";
                    await site.UpdateAppSettings();
                    await site.UpdateConfig(new { properties = new { scmType = "LocalGit" } });
                    resourceGroup.IsRbacEnabled = await rbacTask;
                    site.FireAndForget();
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

                SimpleTrace.Analytics.Information(AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName,
                    userIdentity, template, resourceGroup.CsmId);
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
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

                // We don't need the original site that we create for Web or Mobile apps, delete it or it'll show up in ibiza
                await resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Select(s => s.Delete()).IgnoreFailures().WhenAll();

                // After a deployment, we have no idea what changes happened in the resource group, we should reload it.
                await resourceGroup.Load();

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                var publicAccessTask = resourceGroup.ApiApps.Select(a => a.SetAccessLevel("PublicAnonymous"));
                resourceGroup.IsRbacEnabled = await rbacTask;
                await publicAccessTask.WhenAll();
                return resourceGroup;
            });
        }

        // ARM
        public async Task<ResourceGroup> ActivateLogicApp(LogicTemplate template, TryWebsitesIdentity userIdentity)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Logic, async resourceGroup =>
            {

                SimpleTrace.Analytics.Information(AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName,
                    userIdentity.Name, template, resourceGroup);
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
                            "Logic", template.Name, resourceGroup.ResourceUniqueId);

                var logicApp = new LogicApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, Guid.NewGuid().ToString().Replace("-", ""))
                {
                    LogicAppName = template.Name,
                    Location = resourceGroup.GeoRegion
                };

                var csmTemplateString = string.Empty;

                using(var reader = new StreamReader(template.CsmTemplateFilePath))
                {
                    csmTemplateString = await reader.ReadToEndAsync();
                }

                csmTemplateString = csmTemplateString.Replace("{{gatewayNameDefaultValue}}", Guid.NewGuid().ToString().Replace("-", "")).Replace("{{logicAppNameDefaultValue}}", logicApp.LogicAppName);

                var deployment = new CsmDeployment
                {
                    DeploymentName = resourceGroup.ResourceUniqueId,
                    SubscriptionId = resourceGroup.SubscriptionId,
                    ResourceGroupName = resourceGroup.ResourceGroupName,
                    CsmTemplate = JsonConvert.DeserializeObject<JToken>(csmTemplateString)
                };

                await deployment.Deploy(block: true);

                // After a deployment, we have no idea what changes happened in the resource group
                // we should reload it.
                // TODO: consider reloading the resourceGroup along with the deployment itself.
                await resourceGroup.Load();

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                //var publicAccessTask = resourceGroup.ApiApps.Select(a => a.SetAccessLevel("PublicAnonymous"));
                resourceGroup.IsRbacEnabled = await rbacTask;
                //await Task.WhenAll(publicAccessTask);
                return resourceGroup;
            });
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
                        SimpleTrace.Diagnostics.Fatal(e, "Error in GetResourceGroup, Count: {Count}", Interlocked.Increment(ref _getResourceGroupErrorCount));
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
                await list.Select(resourceGroup =>
                {
                    SimpleTrace.Diagnostics.Information("Deleting resourceGroup {resourceGroupId}", resourceGroup.CsmId);
                    return DeleteResourceGroup(resourceGroup);
                }).WhenAll();
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
