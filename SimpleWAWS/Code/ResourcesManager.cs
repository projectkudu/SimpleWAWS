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
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Trace;
using System.Web.Hosting;

namespace SimpleWAWS.Code
{
    public class ResourcesManager
    {
        public static TimeSpan ResourceGroupExpiryTime;

        private readonly ConcurrentQueue<ResourceGroup> _freeResourceGroups = new ConcurrentQueue<ResourceGroup>();
        private readonly ConcurrentDictionary<string, InProgressOperation> _resourceGroupsInProgress = new ConcurrentDictionary<string, InProgressOperation>();
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
            var csmSubscriptions = await CsmManager.GetSubscriptionNamesToIdMap();
            var subscriptions = await SimpleSettings.Subscriptions.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                //It can be either a displayName or a subscriptionId
                .Select(s => s.Trim())
                .Where(n =>
                {
                    Guid temp;
                    return csmSubscriptions.ContainsKey(n) || Guid.TryParse(n, out temp);
                })
                .Select(sn =>
                {
                    Guid temp;
                    if (Guid.TryParse(sn, out temp)) return sn;
                    else return csmSubscriptions[sn];
                })
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
                try
                {
                    var newResourceGroup = await resourceGroup.DeleteAndCreateReplacement();
                    if (newResourceGroup != null)
                        this._freeResourceGroups.Enqueue(newResourceGroup);
                }
                catch (Exception e)
                {
                    SimpleTrace.Diagnostics.Error(e, "QueueBackgroundWorkItem");
                }
            });
        }

        // ARM
        private async Task<ResourceGroup> ActivateResourceGroup(TryWebsitesIdentity userIdentity, AppService appService, DeploymentType deploymentType, Func<ResourceGroup, InProgressOperation, Task<ResourceGroup>> func)
        {
            ResourceGroup resourceGroup = null;
            if (userIdentity == null)
            {
                throw new InvalidUserIdentityException();
            }

            var userId = userIdentity.Name;
            try
            {
                if (_resourceGroupsInUse.TryGetValue(userId, out resourceGroup))
                {
                    throw new MoreThanOneResourceGroupException();
                }

                if (_freeResourceGroups.TryDequeue(out resourceGroup))
                {
                    //mark site in use as soon as it's checked out so that if there is a reload it will be sorted out to the used queue.
                    await resourceGroup.MarkInUse(userId, ResourceGroupExpiryTime, appService);
                    var rbacTask = Task.FromResult(false); //RbacHelper.AddRbacUser(userIdentity.Puid, userIdentity.Email, resourceGroup);
                    var process = new InProgressOperation(resourceGroup, deploymentType);
                    _resourceGroupsInProgress.AddOrUpdate(userId, s => process, (s, task) => process);
                    SimpleTrace.Diagnostics.Information("resourceGroup {resourceGroupId} is now in use", resourceGroup.CsmId);

                    resourceGroup = await func(resourceGroup, process);

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
                        throw new MoreThanOneResourceGroupException();
                    }
                }
                else
                {
                    throw new NoFreeResourceGroupsException();
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
            catch (InvalidGithubRepoException)
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
                InProgressOperation temp;
                _resourceGroupsInProgress.TryRemove(userId, out temp);
                temp.Complete();
                LogQueueStatistics();
            }
            //if we are here that means a bad exception happened above, but we might leak a site if we don't remove the site and replace it correctly.
            if (resourceGroup != null)
            {
                //no need to await this call
                //this call is to fix our internal state, return an error right away to the caller
                ThreadPool.QueueUserWorkItem(async o => await DeleteResourceGroup(resourceGroup).IgnoreFailure());
            }
            throw new Exception(Resources.Server.Error_GeneralErrorMessage);
        }

        // ARM
        public async Task<ResourceGroup> ActivateWebApp(WebsiteTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName, AppService temp = AppService.Web)
        {
            // Start site specific stuff
            var deploymentType = template != null && template.GithubRepo != null
                ? DeploymentType.GitWithCsmDeploy
                : DeploymentType.ZipDeploy;
            return await ActivateResourceGroup(userIdentity, temp, deploymentType, async (resourceGroup, inProgressOperation) =>
                {
                    SimpleTrace.Analytics.Information(AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName,
                        userIdentity, template, resourceGroup.CsmId);
                    SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}; {5}; {6}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
                            template.Language, template.Name, resourceGroup.ResourceUniqueId, temp.ToString(), anonymousUserName);

                    var site = resourceGroup.Sites.First(s => s.IsSimpleWAWSOriginalSite);
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
                    else if (template != null && template.GithubRepo != null)
                    {
                        Uri githubRepo;
                        var validUri = Uri.TryCreate(template.GithubRepo, UriKind.Absolute, out githubRepo);
                        if (validUri && (githubRepo.AbsoluteUri.StartsWith("https://github.com/davidebbo-test/") || githubRepo.AbsoluteUri.StartsWith("https://github.com/ahmelsayed-test")))
                        {
                            //Do CSM template deployment
                            var csmTemplate = new CsmTemplateWrapper
                            {
                                properties = new CsmTemplateProperties
                                {
                                    mode = "Incremental",
                                    parameters = new
                                    {
                                        siteName = new CsmTemplateParameter(site.SiteName),
                                        hostingPlanName = new CsmTemplateParameter(resourceGroup.ServerFarms.Select(sf => sf.ServerFarmName).FirstOrDefault()),
                                        repoUrl = new CsmTemplateParameter(githubRepo.AbsoluteUri)
                                    },
                                    templateLink = new CsmTemplateLink
                                    {
                                        contentVersion = "1.0.0.0",
                                        uri = new Uri("https://raw.githubusercontent.com/" + githubRepo.AbsolutePath.Trim('/') + "/master/azuredeploy.json")
                                    }
                                }
                            };
                            await inProgressOperation.CreateDeployment(csmTemplate, block: true);
                            await site.GetKuduDeploymentStatus(block: true);
                            await resourceGroup.Load();
                        }
                        else if (validUri && githubRepo.AbsoluteUri.StartsWith("https://github.com/"))
                        {
                            //Do Kudu deployment
                            throw new InvalidGithubRepoException();
                        }
                        else
                        {
                            throw new InvalidGithubRepoException();
                        }
                    }
                    site.AppSettings["LAST_MODIFIED_TIME_UTC"] = DateTime.UtcNow.ToString("u");
                    site.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = SimpleSettings.SiteExpiryMinutes;
                    site.AppSettings["MONACO_EXTENSION_VERSION"] = "beta";
                    site.AppSettings["WEBSITE_TRY_MODE"] = "1";

                    if (template.Name.Equals("ASP.NET + Azure Search Site", StringComparison.OrdinalIgnoreCase))
                    {
                        site.AppSettings["SearchServiceName"] = SimpleSettings.SearchServiceName;
                        site.AppSettings["SearchServiceApiKey"] = AzureSearchHelper.GetApiKey();
                    }

                    await site.UpdateAppSettings();

                    if (template.GithubRepo == null)
                    {
                        await site.UpdateConfig(new { properties = new { scmType = "LocalGit", httpLoggingEnabled = true } });
                    }

                    resourceGroup.IsRbacEnabled = await rbacTask;
                    site.FireAndForget();
                    return resourceGroup;
                });
        }

        // ARM
        public async Task<ResourceGroup> ActivateMobileApp(WebsiteTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateWebApp(template, userIdentity, anonymousUserName, AppService.Mobile);
        }

        // ARM
        public async Task<ResourceGroup> ActivateApiApp(ApiTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Api, DeploymentType.CsmDeploy, async (resourceGroup, inProgressOperation) =>
            {

                SimpleTrace.Analytics.Information(AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName,
                    userIdentity, template, resourceGroup.CsmId);
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}; {5}; {6}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
                            "Api", template.ApiTemplateName, resourceGroup.ResourceUniqueId, AppService.Api.ToString(), anonymousUserName);

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

                await inProgressOperation.CreateDeployment(templateWrapper, block: true);

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
        public async Task<ResourceGroup> ActivateLogicApp(LogicTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Logic, DeploymentType.CsmDeploy, async (resourceGroup, inProgressOperation) =>
            {

                SimpleTrace.Analytics.Information(AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName,
                    userIdentity.Name, template, resourceGroup);
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}; {5}; {6}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, userIdentity.Name,
                            "Logic", template.Name, resourceGroup.ResourceUniqueId, AppService.Logic.ToString(), anonymousUserName);

                var logicApp = new LogicApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, Guid.NewGuid().ToString().Replace("-", ""))
                {
                    Location = resourceGroup.GeoRegion
                };

                var csmTemplateString = string.Empty;

                using(var reader = new StreamReader(template.CsmTemplateFilePath))
                {
                    csmTemplateString = await reader.ReadToEndAsync();
                }

                csmTemplateString = csmTemplateString.Replace("{{gatewayName}}", resourceGroup.Gateways.Select(g => g.GatewayName).First()).Replace("{{logicAppName}}", logicApp.LogicAppName);
                //csmTemplateString = csmTemplateString.Replace("{{gatewayName}}", Guid.NewGuid().ToString().Replace("-", "")).Replace("{{logicAppName}}", logicApp.LogicAppName);

                await inProgressOperation.CreateDeployment(JsonConvert.DeserializeObject<JToken>(csmTemplateString), block: true);

                // After a deployment, we have no idea what changes happened in the resource group
                // we should reload it.
                // TODO: consider reloading the resourceGroup along with the deployment itself.
                await resourceGroup.Load();

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                resourceGroup.IsRbacEnabled = await rbacTask;
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
                InProgressOperation temp;
                if (_resourceGroupsInProgress.TryGetValue(userId, out temp))
                {
                    try
                    {
                        await temp.Task;
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
            return _resourceGroupsInUse.Select(s => s.Value).ToList();
        }

        public IReadOnlyCollection<InProgressOperation> GetAllInProgressResourceGroups()
        {
            return this._resourceGroupsInProgress.Select(s => s.Value).ToList();
        }

        public async Task<string> GetResourceStatusAsync(string userId)
        {
            InProgressOperation inProgressOperation;
            if (this._resourceGroupsInProgress.TryGetValue(userId, out inProgressOperation))
            {
                switch (inProgressOperation.DeploymentType)
                {
                    case DeploymentType.CsmDeploy:
                        return await inProgressOperation.Deployment.GetStatus();
                    case DeploymentType.GitNoCsmDeploy:
                        return Resources.Server.Deployment_GitDeploymentInProgress;
                    case DeploymentType.GitWithCsmDeploy:
                        return "ARM and git deployment in progress";
                    case DeploymentType.ZipDeploy:
                    default:
                        return Resources.Server.Deployment_DeploymentInProgress;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private void LogQueueStatistics()
        {
        }
    }

}
