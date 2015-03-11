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
using SimpleWAWS.Code;
using SimpleWAWS.Authentication;

namespace SimpleWAWS.Controllers
{
    public class SiteController : ApiController
    {
        public async Task<HttpResponseMessage> GetSite()
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            return Request.CreateResponse(HttpStatusCode.OK, await siteManager.GetSite(HttpContext.Current.User.Identity.Name));
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Reset()
        {
            SecurityManager.EnsureAdmin(HttpContext.Current);
            var siteManager = await SiteManager.GetInstanceAsync();
            await siteManager.ResetAllFreeSites();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> DropAndReloadFromAzure()
        {
            SecurityManager.EnsureAdmin(HttpContext.Current);
            var siteManager = await SiteManager.GetInstanceAsync();
            await siteManager.DropAndReloadFromAzure();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        [HttpGet]
        public async Task<HttpResponseMessage> All()
        {
            SecurityManager.EnsureAdmin(HttpContext.Current);
            var siteManager = await SiteManager.GetInstanceAsync();
            var freeSites = siteManager.GetAllFreeSites();
            var inUseSites = siteManager.GetAllInUseSites();
            var inProgressCount = siteManager.GetAllInProgressSitesCount();
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

        public async Task<HttpResponseMessage> GetPublishingProfile()
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var site = await siteManager.GetSite(HttpContext.Current.User.Identity.Name);
            response.Content = await site.GetPublishingProfile();
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = string.Format("{0}.publishsettings", site.Name) };
            return response;
        }

        public async Task<HttpResponseMessage> GetMobileClientZip(string platformString)
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            var site = await siteManager.GetSite(HttpContext.Current.User.Identity.Name);
            MobileClientPlatform platform;
            if (site.AppService != AppService.Mobile)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Wrong app service");
            }

            if (!Enum.TryParse<MobileClientPlatform>(platformString, out platform))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Unsupported platform");
            }

            var response = Request.CreateResponse();
            var replacement = new Dictionary<string, string> {{"ZUMOAPPURL", site.Url}};
            response.Content = MobileHelper.CreateClientZip(platform, replacement);
            return response;
        }

        public async Task<HttpResponseMessage> CreateSite(Template template)
        {
            //Template names should be unique even between languages
            template = TemplatesManager.GetTemplates()
                                       .FirstOrDefault(t => t.Name == template.Name);
            template = template ?? Template.EmptySiteTemplate;

            try
            {
                var siteManager = await SiteManager.GetInstanceAsync();
                if ((await siteManager.GetSite(HttpContext.Current.User.Identity.Name)) != null)
                {
                    Trace.TraceError("{0}; {1}; {2}", AnalyticsEvents.UserGotError,
                        HttpContext.Current.User.Identity.Name, "You can't have more than 1 free site at a time");

                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                        "You can't have more than 1 free site at a time");
                }
                var site = await siteManager.ActivateSiteAsync(template, HttpContext.Current.User.Identity);

                Trace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                    AnalyticsEvents.UserCreatedSiteWithLanguageAndTemplateName, HttpContext.Current.User.Identity.Name,
                    template.Language, template.Name, site.SiteUniqueId);

                return Request.CreateResponse(HttpStatusCode.OK, site);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0}; {1}; {2}", AnalyticsEvents.UserGotError, HttpContext.Current.User.Identity.Name, ex.Message);
                return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable, ex.Message);
            }
        }

        public async Task<HttpResponseMessage> DeleteSite()
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            await siteManager.DeleteSite(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}