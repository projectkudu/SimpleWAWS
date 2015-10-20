using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace SimpleWAWS.Controllers
{
    public class ResourceController : ApiController
    {
        private static int _userGotErrorErrorCount = 0;
        public async Task<HttpResponseMessage> GetResource()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.OK, resourceGroup == null ? null : resourceGroup.UIResource);
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
        public Task<HttpResponseMessage> All()
        {
            return SecurityManager.AdminOnly(async () =>
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();
                var freeSites = resourceManager.GetAllFreeResourceGroups();
                var inUseSites = resourceManager.GetAllInUseResourceGroups();
                var inProgress = resourceManager.GetAllInProgressResourceGroups();
                var backgroundOperations = resourceManager.GetAllBackgroundOperations();
                return Request.CreateResponse(HttpStatusCode.OK,
                    new
                    {
                        freeSiteCount = freeSites.Count(),
                        inProgressSitesCount = inProgress.Count(),
                        inUseSitesCount = inUseSites.Count(),
                        backgroundOperationsCount = backgroundOperations.Count(),
                        //freeSites = freeSites,
                        inUseSites = inUseSites,
                        inProgress = inProgress,
                        backgroundOperations = backgroundOperations
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

        public async Task<HttpResponseMessage> GetMobileClientZip(string platformString, string templateName)
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            MobileClientPlatform platform;
            if (resourceGroup.AppService != AppService.Mobile)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Server.Error_InvalidAppServiceType);
            }

            if (!Enum.TryParse<MobileClientPlatform>(platformString, out platform))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Server.Error_UnsupportedPlatform);
            }

            var response = Request.CreateResponse();
            var replacement = new Dictionary<string, string> 
            {
                { "ZUMOAPPURL", resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).First().Url },
                { "{siteurl}", resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).First().Url.Trim('/') },
                { "ZUMOAPPNAME", "TryMobileApp" },
                { "{sitename}", "TryMobileApp" },
                { "ZUMOGATEWAYURL", resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).First().Url },
                { "{gateway_url}", resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).First().Url.Trim('/') },
                { "ZUMONETRUNTIMESERVERPORT", "44300" }
            };
            response.Content = MobileHelper.CreateClientZip(platform, templateName, replacement);
            return response;
        }

        public async Task<HttpResponseMessage> CreateResource(BaseTemplate template)
        {
            if (template.Name != null && !template.Name.Equals("Github Repo"))
            {
                template = TemplatesManager.GetTemplates()
                    .FirstOrDefault(t => t.Name == template.Name && t.AppService == template.AppService);

                template = template ?? WebsiteTemplate.EmptySiteTemplate;
            }
            else if (template.Name != null && template.Name.Equals("Github Repo"))
            {
                template = new WebsiteTemplate
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
                    SimpleTrace.Diagnostics.Fatal(AnalyticsEvents.MoreThanOneError, identity, 1);
                    //This should use the server version of the error, but due to a string bug they are not the same.
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Client.Information_YouCantHaveMoreThanOne);
                }

                ResourceGroup resourceGroup = null;

                switch (template.AppService)
                {
                    case AppService.Web:
                        resourceGroup = await resourceManager.ActivateWebApp(template as WebsiteTemplate, identity, anonymousUserName);
                        break;
                    case AppService.Mobile:
                        resourceGroup = await resourceManager.ActivateMobileApp(template as WebsiteTemplate, identity, anonymousUserName);
                        break;
                    case AppService.Api:
                        if (identity.Issuer == "OrgId")
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Server.Error_OrgIdNotSupported);
                        }
                        else if (identity.Issuer != "MSA")
                        {
                            return SecurityManager.RedirectToAAD(template.CreateQueryString());
                        }
                        resourceGroup = await resourceManager.ActivateApiApp(template as ApiTemplate, identity, anonymousUserName);
                        break;
                    case AppService.Logic:
                        if (identity.Issuer == "OrgId")
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Server.Error_OrgIdNotSupported);
                        }
                        else if (identity.Issuer != "MSA")
                        {
                            return SecurityManager.RedirectToAAD(template.CreateQueryString());
                        }
                        resourceGroup = await resourceManager.ActivateLogicApp(template as LogicTemplate, identity, anonymousUserName);
                        break;
                }

                return Request.CreateResponse(HttpStatusCode.OK, resourceGroup == null ? null : resourceGroup.UIResource);
            }
            catch (Exception ex)
            {
                var message = ex is NullReferenceException ? Resources.Server.Error_GeneralErrorMessage : ex.Message;
                SimpleTrace.Diagnostics.Fatal(ex, AnalyticsEvents.UserGotError, identity, message, Interlocked.Increment(ref _userGotErrorErrorCount));
                return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable, message);
            }
        }

        public async Task<HttpResponseMessage> DeleteResource()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            resourceManager.DeleteResourceGroup(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.Accepted);
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
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}", AnalyticsEvents.ExtendTrial, resourceGroup.UserId, resourceGroup.AppService.ToString(), resourceGroup.UIResource.TemplateName);
                return Request.CreateResponse(HttpStatusCode.OK, resourceGroup.UIResource);
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
    }
}