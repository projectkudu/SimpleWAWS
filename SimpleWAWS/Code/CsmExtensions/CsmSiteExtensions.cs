using SimpleWAWS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<Site> LoadAppSettings(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.GetSiteAppSettings.Bind(site));
            var config = await response.Content.ReadAsAsync<CsmWrapper<IEnumerable<CsmNameValuePair>>>();

            site.AppSettings = config.properties.ToDictionary(k => k.name, v => v.value);

            var properties = site.AppSettings.Select(e => new { name = e.Key, value = e.Value });
            response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteAppSettings.Bind(site), new { properties = properties });
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> UpdateAppSettings(this Site site)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteAppSettings.Bind(site), new { properties = site.AppSettings });
            csmResponse.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> LoadMetadata(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.GetSiteMetadata.Bind(site));
            var config = await response.Content.ReadAsAsync<CsmWrapper<IEnumerable<CsmNameValuePair>>>();

            site.Metadata = config.properties.ToDictionary(k => k.name, v => v.value);

            var properties = site.Metadata.Select(e => new { name = e.Key, value = e.Value });
            response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteMetadata.Bind(site), new { properties = properties });
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> UpdateMetadata(this Site site)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.PutSiteMetadata.Bind(site), new { properties = site.Metadata});
            csmResponse.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> Load(this Site site, CsmWrapper<CsmSite> csmSite = null)
        {
            Validate.ValidateCsmSite(site);

            if (csmSite == null)
            {
                var csmSiteResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.Site.Bind(site));
                csmSiteResponse.EnsureSuccessStatusCode();
                csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();
            }

            site.HostName = csmSite.properties.hostNames.FirstOrDefault();
            site.ScmHostName = csmSite.properties.enabledHostNames.FirstOrDefault(h => h.IndexOf(".scm.", StringComparison.OrdinalIgnoreCase) != -1);

            await Task.WhenAll(LoadAppSettings(site), LoadMetadata(site), UpdateConfig(site, new { properties = new { scmType = "LocalGit" } }));
            return site;
        }

        public static async Task<Site> Update(this Site site, object update)
        {
            Validate.ValidateCsmSite(site);
            Validate.NotNull(update, "update");

            var response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.Site.Bind(site), update);
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Site> UpdateConfig(this Site site, object config)
        {
            Validate.ValidateCsmSite(site);
            Validate.NotNull(config, "config");

            var response = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.SiteConfig.Bind(site), config);
            response.EnsureSuccessStatusCode();

            return site;
        }

        public static async Task<Stream> GetPublishingProfile(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.SitePublishingProfile.Bind(site));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
    }
}