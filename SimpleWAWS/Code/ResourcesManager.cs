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
using System.Globalization;

namespace SimpleWAWS.Code
{
    public class ResourcesManager
    {

        private readonly BackgroundQueueManager _backgroundQueueManager = new BackgroundQueueManager();
        private static readonly AsyncLock _lock = new AsyncLock();

        private static ResourcesManager _instance;

        //private static int _stateInconsistencyErrorCount = 0;
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
        }

        // ARM
        private async Task LoadAzureResources()
        {
            // Load all subscriptions
            var csmSubscriptions = await CsmManager.GetSubscriptionNamesToIdMap();
            var subscriptions = SimpleSettings.Subscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
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
                });
            HostingEnvironment.QueueBackgroundWorkItem(_ =>
            {
                foreach (var subscription in subscriptions)
                {
                    _backgroundQueueManager.LoadSubscription(subscription);
                }
            });
        }

        // ARM
        private void DeleteResourceGroup(ResourceGroup resourceGroup)
        {
            SimpleTrace.Diagnostics.Information("Deleting expired resourceGroup {resourceGroupId}", resourceGroup.CsmId);
            HostingEnvironment.QueueBackgroundWorkItem(_ => _backgroundQueueManager.DeleteResourceGroup(resourceGroup));
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
                if (_backgroundQueueManager.ResourceGroupsInUse.TryGetValue(userId, out resourceGroup))
                {
                    throw new MoreThanOneResourceGroupException();
                }

                if (_backgroundQueueManager.FreeResourceGroups.TryDequeue(out resourceGroup))
                {
                    //mark site in use as soon as it's checked out so that if there is a reload it will be sorted out to the used queue.
                    await resourceGroup.MarkInUse(userId, appService);
                    var rbacTask = Task.FromResult(false); //RbacHelper.AddRbacUser(userIdentity.Puid, userIdentity.Email, resourceGroup);
                    var process = new InProgressOperation(resourceGroup, deploymentType);
                    _backgroundQueueManager.ResourceGroupsInProgress.AddOrUpdate(userId, s => process, (s, task) => process);
                    SimpleTrace.Diagnostics.Information("resourceGroup {resourceGroupId} is now in use", resourceGroup.CsmId);

                    resourceGroup = await func(resourceGroup, process);

                    var addedResourceGroup = _backgroundQueueManager.ResourceGroupsInUse.GetOrAdd(userId, resourceGroup);
                    if (addedResourceGroup.ResourceGroupName == resourceGroup.ResourceGroupName)
                    {
                        //this means we just added the resourceGroup for the user.
                        await addedResourceGroup.MarkInUse(userId, appService);
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
                if (_backgroundQueueManager.ResourceGroupsInProgress.TryRemove(userId, out temp))
                {
                    temp.Complete();
                    LogQueueStatistics();
                }
            }
            //if we are here that means a bad exception happened above, but we might leak a site if we don't remove the site and replace it correctly.
            if (resourceGroup != null)
            {
                DeleteResourceGroup(resourceGroup);
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
                    resourceGroup.Tags[Constants.TemplateName] = template.Name;
                    site.AppSettings["LAST_MODIFIED_TIME_UTC"] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                    site.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = SimpleSettings.SiteExpiryMinutes;
                    site.AppSettings["MONACO_EXTENSION_VERSION"] = "beta";
                    site.AppSettings["WEBSITE_TRY_MODE"] = "1";

                    if (template.Name.Equals("ASP.NET + Azure Search Site", StringComparison.OrdinalIgnoreCase))
                    {
                        site.AppSettings["SearchServiceName"] = SimpleSettings.SearchServiceName;
                        site.AppSettings["SearchServiceApiKey"] = AzureSearchHelper.GetApiKey();
                    }
                    else if (template.Name.Equals("PHP Starter Site", StringComparison.OrdinalIgnoreCase))
                    {
                        //Enable ZRay
                        await site.EnableZRay(resourceGroup.GeoRegion);
                    }

                    await Task.WhenAll(site.UpdateAppSettings(), resourceGroup.Update());

                    if (template.GithubRepo == null)
                    {
                        await site.UpdateConfig(new { properties = new { scmType = "LocalGit", httpLoggingEnabled = true } });
                    }

                    resourceGroup.IsRbacEnabled = await rbacTask;
                    site.FireAndForget();
                    return resourceGroup;
                });
        }

        public async Task ExtendResourceExpirationTime(ResourceGroup resourceGroup)
        {
            if (resourceGroup.IsExtended)
            {
                throw new ResourceCanOnlyBeExtendedOnce();
            }

            await resourceGroup.ExtendExpirationTime();
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
            _backgroundQueueManager.ResourceGroupsInUse.TryGetValue(userId, out resourceGroup);
            if (resourceGroup == null)
            {
                InProgressOperation temp;
                if (_backgroundQueueManager.ResourceGroupsInProgress.TryGetValue(userId, out temp))
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
                    _backgroundQueueManager.ResourceGroupsInUse.TryGetValue(userId, out resourceGroup);
                }
            }
            return resourceGroup;
        }

        // ARM
        public async Task ResetAllFreeResourceGroups()
        {
            using (await _lock.LockAsync())
            {
                while (!_backgroundQueueManager.FreeResourceGroups.IsEmpty)
                {
                    ResourceGroup temp;
                    if (_backgroundQueueManager.FreeResourceGroups.TryDequeue(out temp))
                    {
                        DeleteResourceGroup(temp);
                    }
                }
            }
        }

        // ARM
        public async Task DropAndReloadFromAzure()
        {
            using (await _lock.LockAsync())
            {
                while (!_backgroundQueueManager.FreeResourceGroups.IsEmpty)
                {
                    ResourceGroup temp;
                    _backgroundQueueManager.FreeResourceGroups.TryDequeue(out temp);
                }
                _backgroundQueueManager.ResourceGroupsInUse.Clear();
                await LoadAzureResources();
            }
        }

        // ARM
        public void DeleteResourceGroup(string userId)
        {
            ResourceGroup resourceGroup;
            _backgroundQueueManager.ResourceGroupsInUse.TryGetValue(userId, out resourceGroup);

            if (resourceGroup != null)
            {
                DeleteResourceGroup(resourceGroup);
            }
        }

        public IReadOnlyCollection<ResourceGroup> GetAllFreeResourceGroups()
        {
            return _backgroundQueueManager.FreeResourceGroups.ToList();
        }

        // ARM
        public IReadOnlyCollection<ResourceGroup> GetAllInUseResourceGroups()
        {
            return _backgroundQueueManager.ResourceGroupsInUse.Select(s => s.Value).ToList();
        }

        public IReadOnlyCollection<InProgressOperation> GetAllInProgressResourceGroups()
        {
            return this._backgroundQueueManager.ResourceGroupsInProgress.Select(s => s.Value).ToList();
        }
        public IReadOnlyCollection<BackgroundOperation> GetAllBackgroundOperations()
        {
            return this._backgroundQueueManager.BackgroundInternalOperations.Select(s => s.Value).ToList();
        }

        public async Task<string> GetResourceStatusAsync(string userId)
        {
            InProgressOperation inProgressOperation;
            if (this._backgroundQueueManager.ResourceGroupsInProgress.TryGetValue(userId, out inProgressOperation))
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
