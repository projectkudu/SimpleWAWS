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
        public async Task<HttpResponseMessage> GetResource()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.OK, resourceGroup == null ? null : resourceGroup.UIResource);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Reset()
        {
            SecurityManager.EnsureAdmin(HttpContext.Current);
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            await resourceManager.ResetAllFreeResourceGroups();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> DropAndReloadFromAzure()
        {
            SecurityManager.EnsureAdmin(HttpContext.Current);
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            await resourceManager.DropAndReloadFromAzure();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> All()
        {
            SecurityManager.EnsureAdmin(HttpContext.Current);
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var freeSites = resourceManager.GetAllFreeResourceGroups();
            var inUseSites = resourceManager.GetAllInUseResourceGroups();
            var inProgressCount = resourceManager.GetAllInProgressResourceGroupsCount();
            return Request.CreateResponse(HttpStatusCode.OK,
                new
                {
                    freeSiteCount = freeSites.Count(),
                    inProgressSitesCount = inProgressCount,
                    inUseSitesCount = inUseSites.Count(),
                    freeSites = freeSites,
                    inUseSites = inUseSites
                });
        }

        public async Task<HttpResponseMessage> GetWebAppPublishingProfile()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            var stream = await resourceGroup.Sites.Select(s => s.GetPublishingProfile()).FirstOrDefault();
            if (stream != null)
            {
                response.Content = new StreamContent(stream);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment") { FileName = string.Format("{0}.publishsettings", resourceGroup.Sites.Select(s => s.SiteName).FirstOrDefault()) };
                return response;
            }
            else
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Didn't get publish profile stream");
            }
        }

        public async Task<HttpResponseMessage> GetMobileClientZip(string platformString)
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            var resourceGroup = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
            MobileClientPlatform platform;
            if (resourceGroup.AppService != AppService.Mobile)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Wrong app service");
            }

            if (!Enum.TryParse<MobileClientPlatform>(platformString, out platform))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Unsupported platform");
            }

            var response = Request.CreateResponse();
            var replacement = new Dictionary<string, string> 
            {
                { "ZUMOAPPURL", resourceGroup.Sites.First().Url },
                { "ZUMOAPPNAME", "TryMobileApp" },
                { "ZUMOGATEWAYURL", resourceGroup.Sites.First().Url },
                { "ZUMONETRUNTIMESERVERPORT", "44300" }
            };
            response.Content = MobileHelper.CreateClientZip(platform, replacement);
            return response;
        }

        public async Task<HttpResponseMessage> CreateResource(BaseTemplate template)
        {
            template = TemplatesManager.GetTemplates()
                .FirstOrDefault(t => t.Name == template.Name && t.AppService == template.AppService);

            template = template ?? WebsiteTemplate.EmptySiteTemplate;

            try
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();

                if ((await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name)) != null)
                {
                    SimpleTrace.TraceError("{0}; {1}; {2}", AnalyticsEvents.UserGotError,
                        HttpContext.Current.User.Identity.Name, "You can't have more than 1 free site at a time");

                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "You can't have more than 1 free site at a time");
                }

                ResourceGroup resourceGroup = null;

                switch (template.AppService)
                {
                    case AppService.Web:
                        resourceGroup = await resourceManager.ActivateWebApp(template as WebsiteTemplate, HttpContext.Current.User.Identity as TryWebsitesIdentity);
                        break;
                    case AppService.Mobile:
                        resourceGroup = await resourceManager.ActivateMobileApp(template as WebsiteTemplate, HttpContext.Current.User.Identity as TryWebsitesIdentity);
                        break;
                    case AppService.Api:
                        if ((HttpContext.Current.User.Identity as TryWebsitesIdentity).Issuer != "AAD")
                            return SecurityManager.RedirectToAAD(template.CreateQueryString());
                        resourceGroup = await resourceManager.ActivateApiApp(template as ApiTemplate, HttpContext.Current.User.Identity as TryWebsitesIdentity);
                        break;
                    case AppService.Logic:
                        if ((HttpContext.Current.User.Identity as TryWebsitesIdentity).Issuer != "AAD")
                            return SecurityManager.RedirectToAAD(template.CreateQueryString());
                        resourceGroup = await resourceManager.ActivateLogicApp(template as LogicTemplate, HttpContext.Current.User.Identity as TryWebsitesIdentity);
                        break;
                }

                return Request.CreateResponse(HttpStatusCode.OK, resourceGroup == null ? null : resourceGroup.UIResource);
            }
            catch (Exception ex)
            {
                SimpleTrace.TraceError("{0}; {1}; {2}", AnalyticsEvents.UserGotError, HttpContext.Current.User.Identity.Name, ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable, ex.Message);
            }
        }

        public async Task<HttpResponseMessage> DeleteResource()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            await resourceManager.DeleteResourceGroup(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}