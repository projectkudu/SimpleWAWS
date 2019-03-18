﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Kudu.Client.Editor;
using Kudu.Client.Zip;
using Newtonsoft.Json;
using SimpleWAWS.Authentication;
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
            //LoadMonitoringToolResources();
            var subscriptions = await CsmManager.GetSubscriptions();
            HostingEnvironment.QueueBackgroundWorkItem(_ =>
            {
                foreach (var subscription in subscriptions)
                {
                    _backgroundQueueManager.LoadSubscription(subscription);
                }
            });
        }

        private void LoadMonitoringToolResources()
        {
            var subscription =  CsmManager.LoadMonitoringToolsSubscription();
            HostingEnvironment.QueueBackgroundWorkItem(_ =>
            {
                    _backgroundQueueManager.LoadMonitoringToolSubscription(subscription);
            });
        }
        
        // ARM
        private void DeleteResourceGroup(ResourceGroup resourceGroup)
        {
            SimpleTrace.Diagnostics.Information("Deleting expired resourceGroup {resourceGroupId}", resourceGroup.CsmId);
            HostingEnvironment.QueueBackgroundWorkItem(_ => _backgroundQueueManager.DeleteResourceGroup(resourceGroup));
        }

        // ARM
        private async Task<ResourceGroup> ActivateResourceGroup(TryWebsitesIdentity userIdentity, AppService appService, DeploymentType deploymentType, Func<ResourceGroup, InProgressOperation, Task<ResourceGroup>> func, string template="")
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
                bool resourceGroupFound = false;
                if (appService == AppService.Containers || appService == AppService.Linux)
                {
                    resourceGroupFound = _backgroundQueueManager.FreeLinuxResourceGroups.TryDequeue(out resourceGroup);
                }
                else if ((appService == AppService.VSCodeLinux))
                {
                    SimpleTrace.TraceInformation($"Searching vscodequeue for template '{template}': Count of templates:{_backgroundQueueManager.FreeVSCodeLinuxResourceGroups.Count} ");

                    if (_backgroundQueueManager.FreeVSCodeLinuxResourceGroups.ContainsKey(template))
                    {
                        var totalTries = 3;
                        var tries = 0;
                        //bool siteFound = false;
                        while (tries++ < totalTries && !resourceGroupFound)
                        {
                            resourceGroupFound = _backgroundQueueManager.FreeVSCodeLinuxResourceGroups[template].TryDequeue(out resourceGroup);
                            if (resourceGroupFound)
                            {
                            //    try
                            //    {
                            //        var a = Dns.GetHostEntry(resourceGroup.Sites.FirstOrDefault().HostName);
                            //        if (a != null)
                            //        {
                            //            siteFound = true;
                            //        }
                            //    }
                            //    catch
                            //    {
                            //        resourceGroupFound = false; 
                            //        SimpleTrace.TraceInformation($"Found ResourceGroup but HostName isnt active '{resourceGroup.ResourceGroupName}' with template {resourceGroup.DeployedTemplateName}");
                            //    }
                                SimpleTrace.TraceInformation($"Found ResourceGroup '{resourceGroup.ResourceGroupName}' with template {resourceGroup.DeployedTemplateName}");
                            }
                            else
                            {
                                SimpleTrace.TraceInformation($"No resource found in free queue for '{template}' ");
                            }
                        }
                    }
                    else
                    {
                        SimpleTrace.TraceInformation($"Queue for VSCode '{template}' is empty");
                        resourceGroupFound = false;
                    }
                }
                else if ((appService != AppService.MonitoringTools))
                {
                    SimpleTrace.TraceInformation($"Checking non Monitoring Tools -> free queue '{template}' ");
                    resourceGroupFound = _backgroundQueueManager.FreeResourceGroups.TryDequeue(out resourceGroup);
                }
                else if ((appService == AppService.MonitoringTools))
                {
                    SimpleTrace.TraceInformation($"No resource found in free queue for '{template}' ");
                    resourceGroup = _backgroundQueueManager.MonitoringResourceGroup;
                    BackgroundQueueManager.MonitoringResourceGroupCheckoutTimes.AddOrUpdate(userId, DateTime.UtcNow,(key, oldValue)=> DateTime.UtcNow);
                    SimpleTrace.Diagnostics.Information("resourceGroup {resourceGroupId} is now assigned", resourceGroup.CsmId);
                    return await func(resourceGroup, null);
                }
                if (resourceGroupFound)
                {
                    //mark site in use as soon as it's checked out so that if there is a reload it will be sorted out to the used queue.
                    await resourceGroup.MarkInUse(userId, appService);
                    //var rbacTask = Task.FromResult(false); //RbacHelper.AddRbacUser(userIdentity.Puid, userIdentity.Email, resourceGroup);
                    var process = new InProgressOperation(resourceGroup, deploymentType);
                    _backgroundQueueManager.ResourceGroupsInProgress.AddOrUpdate(userId, s => process, (s, task) => process);
                    SimpleTrace.Diagnostics.Information("site {siteId} is now in use", String.Concat( resourceGroup.CsmId, "/" ,resourceGroup.Sites.FirstOrDefault(s => s.IsSimpleWAWSOriginalSite).SiteName));

                    resourceGroup = await func(resourceGroup, process);

                    var addedResourceGroup = _backgroundQueueManager.ResourceGroupsInUse.GetOrAdd(userId, resourceGroup);
                    if (addedResourceGroup.ResourceGroupName == resourceGroup.ResourceGroupName)
                    {
                        //this means we just added the resourceGroup for the user.
                        //Removing this line since we have already marked the resourcegroup as in use by the user
                        //await addedResourceGroup.MarkInUse(userId, appService);
                        return addedResourceGroup;
                    }
                    else
                    {
                        //this means the user is trying to add more than 1 site.
                        //delete the new site that's not yet added to the used list
                        SimpleTrace.Diagnostics.Information("User asked for more than 1 site. Replacing {resourceGroup.CsmId}", resourceGroup.CsmId);
                        await resourceGroup.DeleteAndCreateReplacement();
                        throw new MoreThanOneResourceGroupException();
                    }
                }
                else
                {
                    SimpleTrace.Diagnostics.Information("No resource group found yet. Shouldnt be here");
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
        public async Task<ResourceGroup> ActivateWebApp(BaseTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName, AppService temp = AppService.Web)
        {
            // Start site specific stuff
            var deploymentType = template != null && template.GithubRepo != null
                ? DeploymentType.GitWithCsmDeploy
                : DeploymentType.ZipDeploy;
            return await ActivateResourceGroup(userIdentity, temp, deploymentType, async (resourceGroup, inProgressOperation) =>                {
                    SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}; ",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, "NA", template.Name, resourceGroup.ResourceUniqueId, temp.ToString());

                    var site = resourceGroup.Sites.First(s => s.IsSimpleWAWSOriginalSite);
                    var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                    if (template != null && template.FileName != null)
                    {
                        var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                        var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials, retryCount: 3);
                        Task zipUpload = zipManager.PutZipFileAsync("site/wwwroot", template.GetFullPath());

                        var vfsSCMManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);
                        Task scmRedirectUpload = vfsSCMManager.Put("site/applicationHost.xdt", Path.Combine(HostingEnvironment.MapPath(@"~/App_Data"), "applicationHost.xdt"));

                        var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);
                        Task deleteHostingStart = vfsManager.Delete("site/wwwroot/hostingstart.html");

                        await Task.WhenAll(zipUpload, scmRedirectUpload, deleteHostingStart);
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
                            await inProgressOperation.CreateDeployment(csmTemplate, block: true, subscriptionType: resourceGroup.SubscriptionType);
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
                    site.AppSettings["WEBSITE_TRY_MODE"] = "1";
                    if (site.SubscriptionType != SubscriptionType.VSCodeLinux)
                    {
                        site.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = SimpleSettings.SiteExpiryMinutes;
                    }
                    if (site.AppSettings.ContainsKey("FUNCTIONS_EXTENSION_VERSION"))
                    {
                        site.AppSettings.Remove("FUNCTIONS_EXTENSION_VERSION");
                    }

                    if (template.Name.Equals("ASP.NET with Azure Search Site", StringComparison.OrdinalIgnoreCase))
                    {
                        site.AppSettings["SearchServiceName"] = SimpleSettings.SearchServiceName;
                        site.AppSettings["SearchServiceApiKey"] = AzureSearchHelper.GetApiKey();
                    }

                    await Task.WhenAll(site.UpdateAppSettings(), resourceGroup.Update());

                    if (template.GithubRepo == null)
                    {
                        if (site.IsFunctionsContainer)
                        {
                            await site.UpdateConfig(new {properties = new {scmType = "None", httpLoggingEnabled = true}});
                        }
                        else if (template.Name.Equals("WordPress", StringComparison.OrdinalIgnoreCase))
                        {
                            await site.UpdateConfig(new {properties = new {scmType = "LocalGit", httpLoggingEnabled = true, localMySqlEnabled = true} });
                        }
                        else
                        {
                            await site.UpdateConfig(new { properties = new { scmType = "LocalGit", httpLoggingEnabled = true } });
                        }
                    }

                    resourceGroup.IsRbacEnabled = await rbacTask;
                    Util.WarmUpSite(site);
                    return resourceGroup;
                });
        }

        public async Task<ResourceGroup> ExtendResourceExpirationTime(ResourceGroup resourceGroup)
        {
            if (resourceGroup.IsExtended)
            {
                throw new ResourceCanOnlyBeExtendedOnce();
            }

            return await resourceGroup.ExtendExpirationTime();
        }

        // ARM
        public async Task<ResourceGroup> ActivateApiApp(BaseTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateWebApp(template, userIdentity, anonymousUserName, AppService.Api);
        }

        // ARM
        public async Task<ResourceGroup> ActivateLogicApp(BaseTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Logic, DeploymentType.CsmDeploy, async (resourceGroup, inProgressOperation) =>
            {
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, "Logic", template.Name, resourceGroup.ResourceUniqueId, AppService.Logic.ToString());

                var logicApp = new LogicApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, Guid.NewGuid().ToString().Replace("-", ""))
                {
                    Location = resourceGroup.GeoRegion
                };

                var csmTemplateString = string.Empty;

                using(var reader = new StreamReader(((SimpleWAWS.Models.LogicTemplate)(TemplatesManager.GetTemplates().FirstOrDefault((a) => a.AppService == AppService.Logic))).CsmTemplateFilePath))
                {
                    csmTemplateString = await reader.ReadToEndAsync();
                }

                csmTemplateString = csmTemplateString.Replace("{{logicAppName}}", logicApp.LogicAppName);
                //csmTemplateString = csmTemplateString.Replace("{{gatewayName}}", Guid.NewGuid().ToString().Replace("-", "")).Replace("{{logicAppName}}", logicApp.LogicAppName);

                await inProgressOperation.CreateDeployment(JsonConvert.DeserializeObject<JToken>(csmTemplateString), block: true, subscriptionType: resourceGroup.SubscriptionType);

                // After a deployment, we have no idea what changes happened in the resource group
                // we should reload it.
                // TODO: consider reloading the resourceGroup along with the deployment itself.
                await resourceGroup.Load();

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                resourceGroup.IsRbacEnabled = await rbacTask;
                return resourceGroup;
            });
        }
        public async Task<ResourceGroup> ActivateMonitoringToolsApp(BaseTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.MonitoringTools, DeploymentType.RbacOnly, async (resourceGroup, inProgressOperation) =>
            {

                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, 
                            "MonitoringTools", template.Name, resourceGroup.ResourceUniqueId, AppService.MonitoringTools.ToString());

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                resourceGroup.IsRbacEnabled = await rbacTask;
                return resourceGroup;
            });
        }
        public async Task<ResourceGroup> ActivateLinuxResource(BaseTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Linux, DeploymentType.CsmDeploy, async (resourceGroup, inProgressOperation) =>
            {
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, "Linux", template.Name, resourceGroup.ResourceUniqueId, AppService.Linux.ToString());

                var site = resourceGroup.Sites.First(s => s.IsSimpleWAWSOriginalSite);
                resourceGroup.Tags[Constants.TemplateName] = template.Name;
                resourceGroup = await resourceGroup.Update();

                await Util.DeployLinuxTemplateToSite(template, site);

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                resourceGroup.IsRbacEnabled = await rbacTask;
                return resourceGroup;
            });
        }
        public async Task<ResourceGroup> ActivateVSCodeLinuxResource(BaseTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.VSCodeLinux, DeploymentType.CsmDeploy, async (resourceGroup, inProgressOperation) =>
            {
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, "VSCodeLinux", template.Name, resourceGroup.CsmId, resourceGroup.Sites.FirstOrDefault().SiteName);

                //var site = resourceGroup.Sites.First(s => s.IsSimpleWAWSOriginalSite);
                //if (template.Name.Equals(Constants.NodejsVSCodeWebAppLinuxTemplateName, StringComparison.OrdinalIgnoreCase))
                //{

                    //try
                    //{
                    //SimpleTrace.TraceError("Adding time stamp");
                    //await Util.AddTimeStampFile(site, resourceGroup.SiteGuid, DateTime.UtcNow.Add(resourceGroup.TimeLeft));
                    //var lsm = new LinuxSiteManager.Client.LinuxSiteManager(retryCount: 2);
                    //Task checkSite = lsm.CheckTimeStampMetaDataDeploymentStatusAsync(site.Url);
                    //await checkSite;
                    //SimpleTrace.TraceError("Time stamp added");
                    //}
                    //catch (Exception ex)
                    //{
                    //    //TODO: Alert on this specifically after we add parsing logic
                    //    SimpleTrace.TraceError("New TimeStamp wasnt deployed" + ex.Message + ex.InnerException?.Message + ex.InnerException?.StackTrace+ ex.StackTrace);
                    //}
                //}
                //else
                //{
                //    await Util.DeployVSCodeLinuxTemplateToSite(template, site, addTimeStampFile:true);
                //}
                return resourceGroup;
            }, template.Name);
        }
        public async Task<ResourceGroup> ActivateContainersResource(ContainersTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Containers,  DeploymentType.CsmDeploy, async (resourceGroup, inProgressOperation) =>
            {

                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName, 
                            "Containers", template.Name, resourceGroup.ResourceUniqueId, AppService.Containers.ToString());

                var site = resourceGroup.Sites.First(s => s.IsSimpleWAWSOriginalSite);
                resourceGroup.Tags[Constants.TemplateName] = template.Name;
                resourceGroup = await resourceGroup.Update();
                if (!string.IsNullOrEmpty(template.DockerContainer))
                {
                    var qualifiedContainerName = QualifyContainerName(template.DockerContainer);
                    await site.UpdateConfig(new { properties = new { linuxFxVersion = qualifiedContainerName } });
                }

                Util.WarmUpSite(resourceGroup.Sites.FirstOrDefault());

                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email);
                resourceGroup.IsRbacEnabled = await rbacTask;
                return resourceGroup;
            });
        }
        private string QualifyContainerName(string containerName)
        {
            if (!containerName.Contains("|"))
                containerName = "DOCKER|" + containerName;
            if (!containerName.Contains(":"))
                containerName = containerName + ":latest";
            return containerName;
        }

        // ARM
        public async Task<ResourceGroup> ActivateFunctionApp(BaseTemplate template, TryWebsitesIdentity userIdentity, string anonymousUserName)
        {
            return await ActivateResourceGroup(userIdentity, AppService.Function, DeploymentType.FunctionDeploy, async (resourceGroup, inProgressOperation) =>
            {
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.OldUserCreatedSiteWithLanguageAndTemplateName,
                            "Function", template.Name, resourceGroup.ResourceUniqueId, AppService.Function.ToString());

                await resourceGroup.Load();
                var functionApp = resourceGroup.Sites.First(s => s.IsFunctionsContainer);
                if (template.Name.Contains(Constants.CSharpLanguage))
                {
                    functionApp.AppSettings[Constants.FunctionsRuntimeAppSetting] = Constants.DotNetRuntime;
                }
                else if (template.Name.Contains(Constants.JavaScriptLanguage))
                {
                    functionApp.AppSettings[Constants.FunctionsRuntimeAppSetting] = Constants.JavaScriptRuntime;
                }

                await functionApp.UpdateAppSettings();

                Util.WarmUpSite(functionApp); 
                var rbacTask = resourceGroup.AddResourceGroupRbac(userIdentity.Puid, userIdentity.Email, isFunctionContainer: true);
                resourceGroup.Tags[Constants.TemplateName] = template.Name;
                await resourceGroup.Update();
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
        public async Task<ResourceGroup> GetResourceGroupFromSiteName(string siteName, string resourceId)
        {
            ResourceGroup resourceGroup;
            resourceGroup = _backgroundQueueManager.LoadedResourceGroups.Where(a => a.Sites.Any(s=> s.SiteName.Equals(siteName,StringComparison.OrdinalIgnoreCase) && s.ResourceGroupName.Equals(resourceId, StringComparison.OrdinalIgnoreCase))).First();
            return resourceGroup;
        }
        // ARM
        public async Task ResetAllFreeResourceGroups()
        {
            using (await _lock.LockAsync())
            {
                foreach (var template in _backgroundQueueManager.FreeVSCodeLinuxResourceGroups.Keys)
                {
                    while (!_backgroundQueueManager.FreeVSCodeLinuxResourceGroups[template].IsEmpty)
                    {
                        ResourceGroup temp;
                        if (_backgroundQueueManager.FreeVSCodeLinuxResourceGroups[template].TryDequeue(out temp))
                        {
                            DeleteResourceGroup(temp);
                        }
                    }
                }
                foreach (var template in _backgroundQueueManager.FreeVSCodeLinuxResourceGroups.Keys)
                {
                    while (!_backgroundQueueManager.FreeLinuxResourceGroups.IsEmpty)
                    {
                        ResourceGroup temp;
                        if (_backgroundQueueManager.FreeLinuxResourceGroups.TryDequeue(out temp))
                        {
                            DeleteResourceGroup(temp);
                        }
                    }
                }
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
        public IEnumerable<ResourceGroup> GetAllLoadedResourceGroups()
        {
            return _instance._backgroundQueueManager.LoadedResourceGroups;
        }

        public IReadOnlyCollection<ResourceGroup> GetAllFreeResourceGroups()
        {
            return _backgroundQueueManager.FreeResourceGroups.ToList();
        }
        public IReadOnlyCollection<ResourceGroup> GetAllFreeLinuxResourceGroups()
        {
            return _backgroundQueueManager.FreeLinuxResourceGroups.ToList();
        }
        public IReadOnlyCollection<ResourceGroup> GetAllFreeVSCodeLinuxResourceGroups()
        {
            return _backgroundQueueManager.FreeVSCodeLinuxResourceGroups.Values.SelectMany(e => e).ToList();
        }
        public ResourceGroup GetMonitoringToolResourceGroup()
        {
            return _backgroundQueueManager.MonitoringResourceGroup;
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
        public double GetUptime()
        {
            return this._backgroundQueueManager._uptime.Elapsed.TotalMinutes;
        }
        public async Task CleanupSubscriptions()
        {
            //resource loading can be monitored at /api/resource
            await this._backgroundQueueManager.CleanupSubscriptions();
        }
        public int GetResourceGroupCleanupCount()
        {
            return this._backgroundQueueManager._cleanupOperationsTriggered;
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
