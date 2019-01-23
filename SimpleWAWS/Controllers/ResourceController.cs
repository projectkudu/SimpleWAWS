using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SimpleWAWS.Models;
using SimpleWAWS.Authentication;
using SimpleWAWS.Code.CsmExtensions;
using System.Threading;
using SimpleWAWS.Code;
using SimpleWAWS.Trace;
using System.IO;
using System.Text;

namespace SimpleWAWS.Controllers
{
    public class ResourceController : ApiController
    {
        private static int _userGotErrorErrorCount = 0;
        public async Task<HttpResponseMessage> GetResource()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            var returning = resourceGroup == null ? null : (HttpContext.Current.Request.QueryString["appServiceName"] == "Function") ? resourceGroup.FunctionsUIResource : UpdateMonitoringToolsTimeLeft(resourceGroup.UIResource);
            try
            {
                SimpleTrace.TraceInformation($"GET Resource. Returning { returning?.SiteName } with template { returning?.TemplateName } for user { ((TryWebsitesIdentity)(HttpContext.Current.User.Identity)).UniqueName}");
            }
            catch (Exception ex)
            {
                SimpleTrace.TraceError($"Error Logging Get Information: {ex.Message} -> {ex.StackTrace}");
            }
            return Request.CreateResponse(HttpStatusCode.OK, returning);
        }
        [HttpGet]
        public async Task<HttpResponseMessage> GetVSCodeResource()
        {
            try
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();
                SimpleTrace.TraceInformation($"GetVSCodeResource called with text loginSessionCookie:{Uri.UnescapeDataString(HttpContext.Current.Request.Cookies[AuthConstants.LoginSessionCookie].Value)}");

                var loginSessionCookie = Uri.UnescapeDataString(HttpContext.Current.Request.Cookies[AuthConstants.LoginSessionCookie].Value);
                SimpleTrace.TraceInformation($"GetVSCodeResource called with loginSessionCookie:{loginSessionCookie}");
                {

                    var resourceGroup = resourceManager.GetAllInUseResourceGroups().ToList().First(a =>a.UIResource.LoginSession.Equals(loginSessionCookie));
                    var returning = resourceGroup == null ? null : (HttpContext.Current.Request.QueryString["appServiceName"] == "Function") ? resourceGroup.FunctionsUIResource : UpdateMonitoringToolsTimeLeft(resourceGroup.UIResource);
                    try
                    {
                        SimpleTrace.TraceInformation($"GET Resource. Returning { returning?.SiteName } with template { returning?.TemplateName } for user { returning?.UserName}");
                    }
                    catch (Exception ex)
                    {
                        SimpleTrace.TraceError($"Error Logging Get Information: {ex.Message} -> {ex.StackTrace}");
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, returning);
                }
            }
            catch (Exception ex){
                return Request.CreateResponse(HttpStatusCode.InternalServerError, $"key:{Uri.UnescapeDataString(HttpContext.Current.Request.Cookies[AuthConstants.LoginSessionCookie].Value)}. {ex.ToString()}");
            }
        }

        [HttpGet]
        public Task<HttpResponseMessage> Reset()
        {
            return SecurityManager.AdminOnly(async () => {

                var resourceManager = await ResourcesManager.GetInstanceAsync();
                await resourceManager.ResetAllFreeResourceGroups();
                return Request.CreateResponse(HttpStatusCode.Accepted);
            });
        }
        [HttpGet]
        public Task<HttpResponseMessage> RunCleanupSubscriptions()
        {
            return SecurityManager.AdminOnly(async () => {

                var resourceManager = await ResourcesManager.GetInstanceAsync();
                await resourceManager.CleanupSubscriptions();
                return Request.CreateResponse(HttpStatusCode.Accepted);
            });
        }
        
        [HttpGet]
        public Task<HttpResponseMessage> DropAndReloadFromAzure()
        {
            return SecurityManager.AdminOnly(async () =>
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();
                await resourceManager.DropAndReloadFromAzure();
                return Request.CreateResponse(HttpStatusCode.Accepted);
            });
        }

        [HttpGet]
        public async Task<HttpResponseMessage> DeleteUserResource(string userIdentity)
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            resourceManager.DeleteResourceGroup(userIdentity);
            return Request.CreateResponse(HttpStatusCode.Accepted,$"Queued up deletes for resource assigned to: {userIdentity}");
        }

        [HttpGet]
        public Task<HttpResponseMessage> All(bool showFreeResources = false)
        {
            return SecurityManager.AdminOnly(async () =>
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();
                var freeSites = resourceManager.GetAllFreeResourceGroups().Where(sub => sub.SubscriptionType == SubscriptionType.AppService);
                var inUseSites = resourceManager.GetAllInUseResourceGroups().Where(sub => sub.SubscriptionType == SubscriptionType.AppService);
                var inUseSitesList = inUseSites as IList<ResourceGroup> ?? inUseSites.ToList();
                var inUseSitesCount = inUseSitesList.Count();
                var inUseFunctionsCount = inUseSitesList.Count(res => res.AppService == AppService.Function);
                var inUseWebsitesCount = inUseSitesList.Count(res => res.AppService == AppService.Web && res.SubscriptionType == SubscriptionType.AppService);
                var inUseContainerCount = inUseSitesList.Count(res => res.AppService == AppService.Containers || (res.SubscriptionType == SubscriptionType.Linux));
                var inUseLogicAppCount = inUseSitesList.Count(res => res.AppService == AppService.Logic);


                var inProgress = resourceManager.GetAllInProgressResourceGroups();
                var backgroundOperations = resourceManager.GetAllBackgroundOperations();
                var freeLinuxResources = resourceManager.GetAllFreeLinuxResourceGroups();
                var inUseLinuxResources = resourceManager.GetAllInUseResourceGroups().Where(sub => sub.SubscriptionType == SubscriptionType.Linux);
                var freeSitesList = freeSites as IList<ResourceGroup> ?? freeSites.ToList();
                var freeLinuxSitesList = freeLinuxResources as IList<ResourceGroup> ?? freeLinuxResources.ToList();
                var inUseLinuxResourcesList = inUseLinuxResources as IList<ResourceGroup> ?? inUseLinuxResources.ToList();

                var freeVSCodeLinuxResources = resourceManager.GetAllFreeVSCodeLinuxResourceGroups();
                var inUseVSCodeLinuxResources = resourceManager.GetAllInUseResourceGroups().Where(sub => sub.SubscriptionType == SubscriptionType.VSCodeLinux);
                var freeVSCodeLinuxSitesList = freeVSCodeLinuxResources as IList<ResourceGroup> ?? freeVSCodeLinuxResources.ToList();
                var inUseVSCodeLinuxResourcesList = inUseVSCodeLinuxResources as IList<ResourceGroup> ?? inUseVSCodeLinuxResources.ToList();

                var uptime = resourceManager.GetUptime();
                var resourceGroupCleanupCount = resourceManager.GetResourceGroupCleanupCount();
                var monitoringToolsResource= resourceManager.GetMonitoringToolResourceGroup();
                return Request.CreateResponse(HttpStatusCode.OK,
                    new
                    {
                        freeSiteCount = freeSitesList.Count(),
                        freeLinuxResourceCount = freeLinuxResources.Count(),
                        freeVSCodeLinuxResourceCount = freeVSCodeLinuxResources.Count(),
                        inUseSitesCount = inUseSitesCount,
                        inUseFunctionsCount = inUseFunctionsCount,
                        inUseWebsitesCount= inUseWebsitesCount,
                        inUseContainerCount= inUseContainerCount,
                        inUseLogicAppCount =inUseLogicAppCount,
                        inUseLinuxResourceCount = inUseLinuxResourcesList.Count(),
                        inUseVSCodeLinuxResourceCount = inUseVSCodeLinuxResourcesList.Count(),
                        inProgressSitesCount = inProgress.Count(),
                        inUseMonitoringToolsUsersCount = BackgroundQueueManager.MonitoringResourceGroupCheckoutTimes.Count,
                        backgroundOperationsCount = backgroundOperations.Count(),
                        freeVSCodeLinuxResources = showFreeResources ? freeVSCodeLinuxSitesList : null,
                        inUseVSCodeLinuxResources = showFreeResources ? inUseVSCodeLinuxResourcesList : null,
                        inUseSites = inUseSitesList,
                        inUseLinuxResources = inUseLinuxResourcesList,
                        freeSites = showFreeResources ? freeSitesList : null,
                        freeLinuxResources = showFreeResources ? freeLinuxSitesList : null,
                        inProgress = inProgress,
                        monitoringToolsResource = monitoringToolsResource,
                        monitoringToolsResourceUsers = BackgroundQueueManager.MonitoringResourceGroupCheckoutTimes,
                        backgroundOperations = backgroundOperations,
                        uptime = uptime,
                        resourceGroupCleanupCount = resourceGroupCleanupCount
                    });
            });
        }

        public async Task<HttpResponseMessage> GetWebAppPublishingProfile()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            var stream = await resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Select(s => s.GetPublishingProfile()).FirstOrDefault();
            if (stream != null)
            {
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment") { FileName = string.Format("{0}.publishsettings", resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Select(s => s.SiteName).FirstOrDefault()) };
                return response;
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, Resources.Server.Error_GettingPublishingProfileStream);
            }
        }

        public async Task<HttpResponseMessage> GetWebAppContent(string siteName)
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var split = siteName.Split('^');
            var tasSiteName = split[0];
            var rgName = split[1];
            var resourceGroup = await resourceManager.GetResourceGroupFromSiteName(tasSiteName, rgName);
            var stream = await resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Select(s => s.GetSiteContent()).FirstOrDefault();
            if (stream != null)
            {
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment") { FileName = "wwwroot.zip" };
                return response;
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, Resources.Server.Error_GettingPublishingProfileStream);
            }
        }
        public async Task<HttpResponseMessage> CreateResource(BaseTemplate template)
        {
            try
            {
                SimpleTrace.TraceInformation($"CREATE {template?.AppService} . Request Received");
            }
            catch (Exception ex)
            {
                SimpleTrace.TraceError($"Error Logging Create Start Information: {ex.Message} -> {ex.StackTrace}");

            }
            var tempTemplate = WebsiteTemplate.EmptySiteTemplate;

            if (template == null)
            {
                template = WebsiteTemplate.EmptySiteTemplate;
            }
            else if (template.AppService.Equals(AppService.Function))
            {
                tempTemplate = FunctionTemplate.DefaultFunctionTemplate(template.Name);
            }
            else if (template.AppService.Equals(AppService.Containers))
            {
                var containersTemplate = ContainersTemplate.GetContainersTemplate(template.Name);
                containersTemplate.DockerContainer = template.DockerContainer;
                tempTemplate = containersTemplate;
            }
            else if (template.Name != null && !template.Name.Equals("Github Repo") && !template.AppService.Equals(AppService.Function))
            {
                var temp = TemplatesManager.GetTemplates()
                    .FirstOrDefault(t => t.Name == template.Name);

                tempTemplate = WebsiteTemplate.DefaultTemplate(temp.Name,temp.AppService,temp.Language,temp.FileName,template.DockerContainer,temp.MSDeployPackageUrl);
            }
            else if (template.Name != null && template.Name.Equals("Github Repo"))
            {
                tempTemplate = new WebsiteTemplate
                {
                    AppService = AppService.Web,
                    GithubRepo = template.GithubRepo,
                    Name = template.Name,
                    Language = "Github"
                };
            }

            var identity = HttpContext.Current.User.Identity as TryWebsitesIdentity;
            var anonymousUserName = SecurityManager.GetAnonymousUserName(new HttpContextWrapper(HttpContext.Current));

            try
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();

                if ((await resourceManager.GetResourceGroup(identity.Name)) != null)
                {
                    SimpleTrace.Diagnostics.Fatal(AnalyticsEvents.MoreThanOneError, 1);
                    //This should use the server version of the error, but due to a string bug they are not the same.
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Client.Information_YouCantHaveMoreThanOne);
                }

                ResourceGroup resourceGroup = null;

                switch (tempTemplate.AppService)
                {
                    case AppService.Linux:
                            resourceGroup = await resourceManager.ActivateLinuxResource(tempTemplate , identity, anonymousUserName);
                        break;
                    case AppService.VSCodeLinux:
                        resourceGroup = await resourceManager.ActivateVSCodeLinuxResource(tempTemplate , identity, anonymousUserName);
                        break;
                    case AppService.Web:
                            resourceGroup = await resourceManager.ActivateWebApp(tempTemplate , identity, anonymousUserName);
                        break;
                    case AppService.Api:
                        resourceGroup = await resourceManager.ActivateApiApp(tempTemplate, identity, anonymousUserName);
                        break;
                    case AppService.Logic:
                        if (identity.Issuer == "OrgId")
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Server.Error_OrgIdNotSupported);
                        }
                        else if (identity.Issuer != "MSA")
                        {
                            return SecurityManager.RedirectToAAD(tempTemplate.CreateQueryString());
                        }
                        resourceGroup = await resourceManager.ActivateLogicApp(tempTemplate, identity, anonymousUserName);
                        break;
                    case AppService.Function:
                        resourceGroup = await resourceManager.ActivateFunctionApp(tempTemplate, identity, anonymousUserName);
                        break;
                    case AppService.Containers:
                        if (identity.Issuer == "OrgId")
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Server.Error_OrgIdNotSupported);
                        }
                        else if (identity.Issuer != "MSA")
                        {
                            return SecurityManager.RedirectToAAD(template.CreateQueryString());
                        }
                        resourceGroup = await resourceManager.ActivateContainersResource(tempTemplate as ContainersTemplate, identity, anonymousUserName);
                        break;
                    case AppService.MonitoringTools:
                        if (identity.Issuer == "OrgId")
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Server.Error_OrgIdNotSupported);
                        }
                        else if (identity.Issuer != "MSA")
                        {
                            return SecurityManager.RedirectToAAD(template.CreateQueryString());
                        }
                        resourceGroup = await resourceManager.ActivateMonitoringToolsApp(tempTemplate as MonitoringToolsTemplate, identity, anonymousUserName);
                        break;
                }
                try
                {
                    SimpleTrace.TraceInformation($"CREATE {template?.AppService}. Returning { GetUIResource(resourceGroup).SiteName } with template { GetUIResource(resourceGroup).TemplateName } for user {identity.UniqueName}");
                }
                catch  (Exception ex)
                {
                    SimpleTrace.TraceError($"Error Logging Create End Information: {ex.Message} -> {ex.StackTrace}");
                }
                return Request.CreateResponse(HttpStatusCode.OK, resourceGroup == null ? null : GetUIResource(resourceGroup) );
            }
            catch (Exception ex)
            {
                var message = ex is NullReferenceException ? Resources.Server.Error_GeneralErrorMessage : ex.Message;
                SimpleTrace.Diagnostics.Fatal(ex, AnalyticsEvents.UserGotError,  message, Interlocked.Increment(ref _userGotErrorErrorCount));
                return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable, message);
            }
        }

        private UIResource GetUIResource(ResourceGroup resourceGroup)
        {
            return string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["appServiceName"])
                   ? resourceGroup.UIResource
                   : ((HttpContext.Current.Request.QueryString["appServiceName"].Equals("Function",
                        StringComparison.InvariantCultureIgnoreCase))
                        ? resourceGroup.FunctionsUIResource
                        : UpdateMonitoringToolsTimeLeft(resourceGroup.UIResource));
        }

        private UIResource UpdateMonitoringToolsTimeLeft(UIResource resourceGroup)
        {
            if (resourceGroup.AppService == AppService.MonitoringTools)
            {
                TimeSpan timeUsed = DateTime.UtcNow - BackgroundQueueManager.MonitoringResourceGroupCheckoutTimes.GetOrAdd(HttpContext.Current.User.Identity.Name, DateTime.UtcNow);
                TimeSpan timeLeft;
                TimeSpan LifeTime = TimeSpan.FromMinutes(int.Parse(SimpleSettings.MonitoringToolsExpiryMinutes));

                if (timeUsed > LifeTime)
                {
                    timeLeft = TimeSpan.FromMinutes(0);
                }
                else
                {
                    timeLeft = LifeTime - timeUsed;
                }
                
                resourceGroup.TimeLeftInSeconds = (int)timeLeft.TotalSeconds;
            }
            return resourceGroup;
        }
        public async Task<HttpResponseMessage> DeleteResource()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            resourceManager.DeleteResourceGroup(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.Accepted, $"Removed any assigned resources to:{ HttpContext.Current.User.Identity.Name }");
        }

        public async Task<HttpResponseMessage> GetResourceStatus()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var status = await resourceManager.GetResourceStatusAsync(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.OK, status);
        }

        public async Task<HttpResponseMessage> ExtendResourceExpirationTime()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            try
            {
                resourceGroup = await resourceManager.ExtendResourceExpirationTime(resourceGroup);
                SimpleTrace.TraceInformation("{0}; {1}", AnalyticsEvents.ExtendTrial, resourceGroup.ResourceUniqueId);
                SimpleTrace.ExtendResourceGroup(resourceGroup);
                return Request.CreateResponse(HttpStatusCode.OK, GetUIResource(resourceGroup));
            }
            catch (ResourceCanOnlyBeExtendedOnce e)
            {
                SimpleTrace.Diagnostics.Error(e, "Resource Extended Once");
                return Request.CreateResponse(HttpStatusCode.BadRequest, e.Message);
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, "Error extending expiration time");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, Resources.Server.Error_GeneralErrorMessage);
            }
        }

        public async Task<HttpResponseMessage> GetVSCodeUrl()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            var stream = resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Select(s => s.GetVSCodeUrl()).FirstOrDefault();
            if (stream != null)
            {
                response.Headers.Location = new Uri(stream);
                response.StatusCode = HttpStatusCode.Redirect;
                return response;
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, Resources.Server.Error_InvalidUserIdentity);
            }
        }
        public async Task<HttpResponseMessage> GetVSCodeInsidersUrl()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            var stream = resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Select(s => s.GetVSCodeInsidersUrl()).FirstOrDefault();
            if (stream != null)
            {
                response.Headers.Location = new Uri(stream);
                response.StatusCode = HttpStatusCode.Redirect;
                return response;
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, Resources.Server.Error_InvalidUserIdentity);
            }
        }
        public async Task<HttpResponseMessage> GetGitCloneUrl()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            var url = resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Select(s => s.GetGitCloneUrl()).FirstOrDefault();
            if (url != null)
            {
                return Request.CreateResponse(HttpStatusCode.OK, url);
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, Resources.Server.Error_InvalidUserIdentity);
            }
        }
    }
}