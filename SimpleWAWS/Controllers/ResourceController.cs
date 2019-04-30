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
            var returning = resourceGroup == null ? null : resourceGroup.UIResource;
            try
            {
                SimpleTrace.TraceInformation($"GET Resource. Returning { returning?.SiteName } with template { returning?.TemplateName } for user { ((TryWebsitesIdentity)(HttpContext.Current.User.Identity)).FilteredName}");
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

                    var resourceGroup = (await resourceManager.GetAllInUseResourceGroups()).First(a => a.UIResource.LoginSession.Equals(loginSessionCookie,StringComparison.InvariantCultureIgnoreCase));
                    var returning = resourceGroup == null ? null : resourceGroup.UIResource;
                    try
                    {
                        SimpleTrace.TraceInformation($"GET Resource. Returning { returning?.SiteName } with template { returning?.TemplateName } for user { ((TryWebsitesIdentity)(HttpContext.Current.User.Identity)).FilteredName}");
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
            await resourceManager.DeleteResourceGroup(userIdentity);
            return Request.CreateResponse(HttpStatusCode.Accepted,$"Queued up deletes for resource assigned to: {userIdentity}");
        }

        [HttpGet]
        public Task<HttpResponseMessage> All(bool showFreeResources = false)
        {
            return SecurityManager.AdminOnly(async () =>
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();
                var freeSites = (await resourceManager.GetAllFreeResourceGroups()).Where(sub => sub.SubscriptionType == SubscriptionType.AppService);
                var inUseSites = (await resourceManager.GetAllInUseResourceGroups()).Where(sub => sub.SubscriptionType == SubscriptionType.AppService);
                var inUseSitesList = inUseSites as IList<ResourceGroup> ?? inUseSites.ToList();
                var inUseSitesCount = inUseSitesList.Count();
                var inUseFunctionsCount = inUseSitesList.Count(res => res.AppService == AppService.Function);
                var inUseWebsitesCount = inUseSitesList.Count(res => res.AppService == AppService.Web && res.SubscriptionType == SubscriptionType.AppService);
                var inUseContainerCount = inUseSitesList.Count(res => res.AppService == AppService.Containers || (res.SubscriptionType == SubscriptionType.Linux));

                var inProgress = resourceManager.GetAllInProgressResourceGroups();
                var backgroundOperations = resourceManager.GetAllBackgroundOperations();
                var freeLinuxResources = (await resourceManager.GetAllFreeResourceGroups()).Where(sub => sub.SubscriptionType == SubscriptionType.Linux);
                var inUseLinuxResources = (await resourceManager.GetAllInUseResourceGroups()).Where(sub => sub.SubscriptionType == SubscriptionType.Linux);
                var freeSitesList = freeSites as IList<ResourceGroup> ?? freeSites.ToList();
                var freeLinuxSitesList = freeLinuxResources as IList<ResourceGroup> ?? freeLinuxResources.ToList();
                var inUseLinuxResourcesList = inUseLinuxResources as IList<ResourceGroup> ?? inUseLinuxResources.ToList();

                var freeVSCodeLinuxResources = (await resourceManager.GetAllFreeResourceGroups()).Where(sub => sub.SubscriptionType == SubscriptionType.VSCodeLinux);
                var inUseVSCodeLinuxResources = (await resourceManager.GetAllInUseResourceGroups()).Where(sub => sub.SubscriptionType == SubscriptionType.VSCodeLinux);
                var freeVSCodeLinuxSitesList = freeVSCodeLinuxResources as IList<ResourceGroup> ?? freeVSCodeLinuxResources.ToList();
                var inUseVSCodeLinuxResourcesList = inUseVSCodeLinuxResources as IList<ResourceGroup> ?? inUseVSCodeLinuxResources.ToList();

                var uptime = resourceManager.GetUptime();
                var resourceGroupCleanupCount = resourceManager.GetResourceGroupCleanupCount();
                return Request.CreateResponse(HttpStatusCode.OK,
                    new
                    {
                        inProgress = inProgress,
                        uptime = uptime,
                        resourceGroupCleanupCount = resourceGroupCleanupCount,
                        freeSiteCount = freeSitesList.Count(),
                        freeLinuxResourceCount = freeLinuxResources.Count(),
                        freeVSCodeLinuxResourceCount = freeVSCodeLinuxResources.Count(),
                        inUseSitesCount = inUseSitesCount,
                        inUseFunctionsCount = inUseFunctionsCount,
                        inUseWebsitesCount= inUseWebsitesCount,
                        inUseContainerCount= inUseContainerCount,
                        inUseLinuxResourceCount = inUseLinuxResourcesList.Count(),
                        inUseVSCodeLinuxResourceCount = inUseVSCodeLinuxResourcesList.Count(),
                        inProgressSitesCount = inProgress.Count(),
                        backgroundOperationsCount = backgroundOperations.Count(),
                        freeVSCodeLinuxResources = showFreeResources ? freeVSCodeLinuxSitesList : null,
                        inUseVSCodeLinuxResources = showFreeResources ? inUseVSCodeLinuxResourcesList : null,
                        inUseSites = inUseSitesList,
                        inUseLinuxResources = inUseLinuxResourcesList,
                        freeSites = showFreeResources ? freeSitesList : null,
                        freeLinuxResources = showFreeResources ? freeLinuxSitesList : null,
                        backgroundOperations = backgroundOperations
                    });
            });
        }

        public async Task<HttpResponseMessage> GetWebAppPublishingProfile()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            var stream = await resourceGroup.Site.GetPublishingProfile();
            if (stream != null)
            {
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment") { FileName = string.Format("{0}.publishsettings", resourceGroup.Site.SiteName) };
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
            var stream = await resourceGroup.Site.GetSiteContent();
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

        public async Task<HttpResponseMessage> GetFunctionAppContent(string siteName)
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var split = siteName.Split('^');
            var tasSiteName = split[0];
            var rgName = split[1];
            var resourceGroup = await resourceManager.GetResourceGroupFromSiteName(tasSiteName, rgName);
            var stream = await resourceGroup.Site.GetSiteContent();
            if (stream != null)
            {
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment") { FileName = tasSiteName + ".zip" };
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
                var containersTemplate = ContainersTemplate.DefaultContainersTemplate(template.Name);
                containersTemplate.DockerContainer = template.DockerContainer;
                tempTemplate = containersTemplate;
            }
            else if (template.Name != null && !template.Name.Equals("GitHub Repo") && !template.AppService.Equals(AppService.Function))
            {
                var temp = TemplatesManager.GetTemplates()
                    .FirstOrDefault(t => t.Name == template.Name);

                tempTemplate = WebsiteTemplate.DefaultTemplate(temp.Name,temp.AppService,temp.Language,temp.FileName,template.DockerContainer,temp.MSDeployPackageUrl);
            }
            else if (template.Name != null && template.Name.Equals("GitHub Repo"))
            {
                tempTemplate = new WebsiteTemplate
                {
                    AppService = AppService.Web,
                    GithubRepo = template.GithubRepo,
                    Name = template.Name,
                    Language = "GitHub"
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
                    case AppService.Function:
                        resourceGroup = await resourceManager.ActivateFunctionApp(tempTemplate, identity, anonymousUserName);
                        break;
                    case AppService.Containers:
                        resourceGroup = await resourceManager.ActivateContainersResource(tempTemplate, identity, anonymousUserName);
                        break;
                }
                try
                {
                    SimpleTrace.TraceInformation($"CREATE {template?.AppService}. Returning { GetUIResource(resourceGroup).SiteName } with template { GetUIResource(resourceGroup).TemplateName } for user {identity.FilteredName}");
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
            return resourceGroup.UIResource;
        }


        public async Task<HttpResponseMessage> DeleteResource()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            await resourceManager.DeleteResourceGroup(HttpContext.Current.User.Identity.Name);
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
            var stream = resourceGroup.Site.GetVSCodeUrl();
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
            var stream = resourceGroup.Site.GetVSCodeInsidersUrl();
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
            var url = resourceGroup.Site.GetGitCloneUrl();
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