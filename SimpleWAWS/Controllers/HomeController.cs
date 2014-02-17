using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SimpleWAWS.Controllers
{
    public class HomeController : Controller
    {
        private const string WAWSSiteCookie = "WAWSSite";
        private const string IdCookieValue = "Id";

        public async Task<ActionResult> Index()
        {
            Site site = await GetCurrentSiteAsync();

            if (site == null)
            {
                return View();
            }

            return View("ShowSite", site);
        }

        [HttpPost]
        public async Task<ActionResult> CreateSite(string submit)
        {
            var siteManager = await SiteManager.GetInstanceAsync();
            Site site = await siteManager.UseSiteAsync();

            HttpCookie cookie = new HttpCookie(WAWSSiteCookie);

            cookie.Values.Add(IdCookieValue, site.Id);

            // Use one-hour expiry since sites are short lived
            cookie.Expires = DateTime.Now.AddHours(1);

            Response.Cookies.Add(cookie);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<ActionResult> DeleteSite()
        {
            string siteId = GetCurrentSiteId();
            if (!String.IsNullOrEmpty(siteId))
            {
                var siteManager = await SiteManager.GetInstanceAsync();
                await siteManager.DeleteSite(siteId);
            }

            return RedirectToAction("Index");
        }

        private async Task<Site> GetCurrentSiteAsync()
        {
            string siteId = GetCurrentSiteId();
            if (String.IsNullOrEmpty(siteId))
            {
                return null;
            }

            var siteManager = await SiteManager.GetInstanceAsync();
            return siteManager.GetSite(siteId);
        }

        private string GetCurrentSiteId()
        {
            HttpCookie cookie = Request.Cookies[WAWSSiteCookie];
            if (cookie == null)
            {
                return null;
            }

            return cookie.Values[IdCookieValue];
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}