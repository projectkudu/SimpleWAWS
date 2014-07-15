using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using Microsoft.ApplicationInsights.Telemetry.Services;
using Newtonsoft.Json;
using SimpleWAWS.Code;

namespace SimpleWAWS.Controllers
{
    public class SiteController : ApiController
    {
        public async Task<HttpResponseMessage> GetSite()
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            return Request.CreateResponse(HttpStatusCode.OK, siteManager.GetSite(HttpContext.Current.User.Identity.Name));
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Reset()
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            await siteManager.ResetAllFreeSites(HttpContext.Current.User.Identity.Name);
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        public async Task<HttpResponseMessage> GetAll()
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            var freeSites = siteManager.GetAllFreeSites();
            var inUseSites = siteManager.GetAllInUseSites();
            return Request.CreateResponse(HttpStatusCode.OK,
                new { freeSiteCount = freeSites.Count(), inUseSitesCount = inUseSites.Count(), freeSites = freeSites, inUseSites = inUseSites});
        }

        public async Task<HttpResponseMessage> GetPublishingProfile()
        {
            ServerAnalytics.CurrentRequest.LogEvent(AppInsightsEvents.UserActions.DownloadPublishingProfile);
            var siteManager = await SiteManager.GetInstanceAsync();
            var response = Request.CreateResponse();
            var site = siteManager.GetSite(HttpContext.Current.User.Identity.Name);
            response.Content = await site.GetPublishingProfile();
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response.Content.Headers.ContentDisposition =
                new ContentDispositionHeaderValue("attachment") { FileName = string.Format("{0}.publishsettings", site.Name) };
            return response;
        }

        public async Task<HttpResponseMessage> CreateSite(Template template)
        {
            var createSiteEvent =
                ServerAnalytics.CurrentRequest.StartTimedEvent(AppInsightsEvents.UserActions.CreateWebsite,
                    new Dictionary<string, object> {{"Template", template.Name}, {"Language", template.Language}});
            try
            {
                var siteManager = await SiteManager.GetInstanceAsync();
                if (siteManager.GetSite(HttpContext.Current.User.Identity.Name) != null)
                {
                    ServerAnalytics.CurrentRequest.LogEvent(AppInsightsEvents.UserErrors.MoreThanOneWebsite);
                    return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable,
                        "Can't have more than 1 free site at a time");
                }
                var site =
                    await
                        siteManager.ActivateSiteAsync(template == null
                            ? null
                            : TemplatesManager.GetTemplates()
                                .SingleOrDefault(t => t.Name == template.Name && t.Language == template.Language),
                            HttpContext.Current.User.Identity.Name);
                return Request.CreateResponse(HttpStatusCode.OK, site);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable, ex.Message);
            }
            finally
            {
                createSiteEvent.End();
            }
        }

        public async Task<HttpResponseMessage> DeleteSite()
        {
            var deleteSiteEvent =
                ServerAnalytics.CurrentRequest.StartTimedEvent(AppInsightsEvents.UserActions.DeleteWebsite);
            var siteManager = await SiteManager.GetInstanceAsync();
            await siteManager.DeleteSite(HttpContext.Current.User.Identity.Name);
            deleteSiteEvent.End();
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}