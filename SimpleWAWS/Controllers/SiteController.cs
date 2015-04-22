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

namespace SimpleWAWS.Controllers
{
    public class SiteController : ApiController
    {
        public async Task<HttpResponseMessage> GetSite()
        {
            var resourceManager = await ResourcesManager.GetInstanceAsync();
            return Request.CreateResponse(HttpStatusCode.OK, await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name));
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

        //public async Task<HttpResponseMessage> GetPublishingProfile()
        //{
        //    var resourceManager = await ResourcesManager.GetInstanceAsync();
        //    var response = Request.CreateResponse();
        //    var site = await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name);
        //    response.Content = await site.GetPublishingProfile();
        //    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        //    response.Content.Headers.ContentDisposition =
        //        new ContentDispositionHeaderValue("attachment") { FileName = string.Format("{0}.publishsettings", site.SiteName) };
        //    return response;
        //}

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

        public async Task<HttpResponseMessage> CreateSite(BaseTemplate template)
        {
            //Template names should be unique even between languages
            template = TemplatesManager.GetTemplates()
                                       .FirstOrDefault(t => t.Name == template.Name);
            template = template ?? WebsiteTemplate.EmptySiteTemplate;

            try
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();
                if ((await resourceManager.GetResourceGroup(HttpContext.Current.User.Identity.Name)) != null)
                {
                    Trace.TraceError("{0}; {1}; {2}", AnalyticsEvents.UserGotError,
                        HttpContext.Current.User.Identity.Name, "You can't have more than 1 free site at a time");

                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "You can't have more than 1 free site at a time");
                }
                ResourceGroup resourceGroup =  await resourceManager.ActivateApiApp(new ApiTemplate { Name = template.Name }, HttpContext.Current.User.Identity as TryWebsitesIdentity);

                //Trace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                //    AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName, HttpContext.Current.User.Identity.Name,
                //    template.Language, template.Name, resourceGroup.ResourceUniqueId);

                return Request.CreateResponse(HttpStatusCode.OK, resourceGroup);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0}; {1}; {2}", AnalyticsEvents.UserGotError, HttpContext.Current.User.Identity.Name, ex.Message);
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