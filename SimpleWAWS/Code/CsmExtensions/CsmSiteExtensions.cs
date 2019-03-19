using Kudu.Client.Editor;
using Kudu.Client.Zip;
using Newtonsoft.Json;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

            var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.GetSiteAppSettings.Bind(site));
            var config = await response.Content.ReadAsAsync<CsmWrapper<IEnumerable<CsmNameValuePair>>>();

            site.AppSettings = config.properties.ToDictionary(k => k.name, v => v.value);

            var properties = site.AppSettings.Select(e => new {name = e.Key, value = e.Value});
            response =
                await
                    GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.PutSiteAppSettings.Bind(site),
                        new {properties = properties});
            await response.EnsureSuccessStatusCodeWithFullError();

            return site;
        }

        public static async Task<Site> UpdateAppSettings(this Site site)
        {
            var csmResponse = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.PutSiteAppSettings.Bind(site), new { properties = site.AppSettings.Select(s => new { name = s.Key, value = s.Value }) });
            await csmResponse.EnsureSuccessStatusCodeWithFullError();

            return site;
        }

        public static async Task<Site> LoadMetadata(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.GetSiteMetadata.Bind(site));
            var config = await response.Content.ReadAsAsync<CsmWrapper<IEnumerable<CsmNameValuePair>>>();

            site.Metadata = config.properties.ToDictionary(k => k.name, v => v.value);

            var properties = site.Metadata.Select(e => new { name = e.Key, value = e.Value });
            response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.PutSiteMetadata.Bind(site), new { properties = properties });
            await response.EnsureSuccessStatusCodeWithFullError();

            return site;
        }
        public static async Task StopSite(this Site site)
        {
            var csmResponse = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.SiteStop.Bind(site), new { });
            try
            {
                await csmResponse.EnsureSuccessStatusCodeWithFullError();
            }
            catch (Exception ex)
            {
                var message = $"Error Stopping site via ARM {site.SiteName} -> {site.ResourceGroupName} -> {site.SubscriptionId} : {ex.Message} -> {ex.StackTrace}";
                SimpleTrace.TraceError(message);
                throw new SiteStartStopFailedException(message);
            }
            if (!await Util.PingTillStatusCode(site.HostName, HttpStatusCode.Forbidden, 35, 5))
            {
                var message = $"Error Pinging site after stop {site.SiteName} -> {site.ResourceGroupName} -> {site.SubscriptionId}";
                SimpleTrace.TraceError(message);
                throw new SiteStartStopFailedException(message);
            }
        }
        public static async Task StartSite(this Site site)
        {
            var csmResponse = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.SiteStart.Bind(site), new { });
            try
            {
                await csmResponse.EnsureSuccessStatusCodeWithFullError();
            }
            catch (Exception ex)
            {
                var message = $"Error Starting site via ARM {site.SiteName} -> {site.ResourceGroupName} -> {site.SubscriptionId} : {ex.Message} -> {ex.StackTrace}";
                SimpleTrace.TraceError(message);
                throw new SiteStartStopFailedException(message);
            }
            if (!await Util.PingTillStatusCode(site.HostName, HttpStatusCode.OK, 35, 5))
            {
                var message = $"Error Pinging site after start {site.SiteName} -> {site.ResourceGroupName} -> {site.SubscriptionId}";
                SimpleTrace.TraceError(message);
                throw new SiteStartStopFailedException(message);
            }
        }
        public static async Task<Site> UpdateMetadata(this Site site)
        {
            var csmResponse = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.PutSiteMetadata.Bind(site), new { properties = site.Metadata });
            await csmResponse.EnsureSuccessStatusCodeWithFullError();

            return site;
        }

        public static async Task<Site> Load(this Site site, CsmWrapper<CsmSite> csmSite = null)
        {
            Validate.ValidateCsmSite(site);
            if (!site.IsSimpleWAWSOriginalSite && !site.IsFunctionsContainer) return site;

            if (csmSite == null)
            {
                var csmSiteResponse = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.Site.Bind(site));
                await csmSiteResponse.EnsureSuccessStatusCodeWithFullError();
                csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();
            }

            site.HostName = csmSite.properties.hostNames.FirstOrDefault();
            site.ScmHostName = csmSite.properties.enabledHostNames.FirstOrDefault(h => h.IndexOf(".scm.", StringComparison.OrdinalIgnoreCase) != -1);

            site.Kind = csmSite.kind;

            await Task.WhenAll(LoadAppSettings(site), LoadPublishingCredentials(site), UpdateScmConfig(site));

            if (!site.IsSimpleWAWSOriginalSite)
            {
                site.AppSettings["FUNCTIONS_EXTENSION_VERSION"] = Constants.FunctionsVersion;
            }
            if (site.SubscriptionType != SubscriptionType.Linux && site.SubscriptionType != SubscriptionType.VSCodeLinux)
            {
                site.AppSettings["WEBSITE_TRY_MODE"] = "1";
                site.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = SimpleSettings.SiteExpiryMinutes;
                site.AppSettings["WEBSITE_NODE_DEFAULT_VERSION"] = SimpleSettings.WebsiteNodeDefautlVersion;
            }
            await site.UpdateAppSettings();
            return site;
        }

        public static async Task UpdateScmConfig(this Site site)
        {
            if (site.IsFunctionsContainer)
            {    await UpdateConfig(site, new
                {
                    properties = new {scmType = "None"}
                });
            }
            else
            {
                await UpdateConfig(site, new
                {
                    properties = new {scmType = "LocalGit"}
                });
            }
        }
        
    public static async Task<Site> LoadPublishingCredentials(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.SitePublishingCredentials.Bind(site));
            var publishingCredentials = await response.Content.ReadAsAsync<CsmWrapper<CsmSitePublishingCredentials>>();

            site.PublishingUserName = publishingCredentials.properties.publishingUserName;
            site.PublishingPassword = publishingCredentials.properties.publishingPassword;

            return site;
        }

        public static async Task<Site> Update(this Site site, object update)
        {
            Validate.ValidateCsmSite(site);
            Validate.NotNull(update, "update");

            var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.Site.Bind(site), update);
            await response.EnsureSuccessStatusCodeWithFullError();

            return site;
        }

        public static async Task<Site> UpdateConfig(this Site site, object config)
        {
            Validate.ValidateCsmSite(site);
            Validate.NotNull(config, "config");

            var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.SiteConfig.Bind(site), config);
            await response.EnsureSuccessStatusCodeWithFullError();

            return site;
        }

        public static async Task<Stream> GetPublishingProfile(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.SitePublishingProfile.Bind(site));
            await response.EnsureSuccessStatusCodeWithFullError();
            return await response.Content.ReadAsStreamAsync();
        }

        public static string GetGitCloneUrl(this Site site)
        {
            Validate.ValidateCsmSite(site);
            return site.GitUrlWithCreds;
        }
        public static string GetVSCodeUrl(this Site site)
        {
            Validate.ValidateCsmSite(site);
            var url = $"vscode://vscode.git/clone?url={HttpUtility.UrlEncode(site.GitUrlWithCreds)}";
            return url;
        }
        public static string GetVSCodeInsidersUrl(this Site site)
        {
            Validate.ValidateCsmSite(site);
            var url = $"vscode-insiders://vscode.git/clone?url={HttpUtility.UrlEncode(site.GitUrlWithCreds)}";
            return url;
        }
        public static async Task<Stream> GetSiteContent(this Site site)
        {
            Validate.ValidateCsmSite(site);
            var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
            var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials, retryCount: 3);
            return await zipManager.GetZipFileStreamAsync("site/wwwroot");

        }

        public static async Task Delete(this Site site)
        {
            Validate.ValidateCsmSite(site);

            var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Delete, ArmUriTemplates.Site.Bind(site));
            await response.EnsureSuccessStatusCodeWithFullError();
        }

        public static async Task<DeployStatus> GetKuduDeploymentStatus(this Site site, bool block)
        {
            Validate.ValidateCsmSite(site);
            while (true)
            {
                DeployStatus? value;
                do
                {
                    var response = await GetClient(site.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.SiteDeployments.Bind(site));
                    await response.EnsureSuccessStatusCodeWithFullError();

                    var deployment = await response.Content.ReadAsAsync<CsmArrayWrapper<CsmSiteDeployment>>();
                    value = deployment.value.Select(s => s.properties.status).FirstOrDefault();

                } while (block && value != DeployStatus.Failed && value != DeployStatus.Success);

                return value.Value;
            }
        }

        private static async Task CreateHostJson(Site site)
        {
            var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
            var vfsManager = new RemoteVfsManager($"{site.ScmUrl}vfs/", credentials, retryCount: 3);
            var hostId = Guid.NewGuid().ToString().Replace("-", "");
            object hostJson = null;
            if (site.IsFunctionsContainer)
            {
                hostJson = new { id = hostId, version = "2.0" };
            }
            else
            {
                hostJson = new { id = hostId };
            }

            var putTask = vfsManager.Put("site/wwwroot/host.json", new StringContent(JsonConvert.SerializeObject(hostJson)));
            var deleteTask = vfsManager.Delete("site/wwwroot/hostingstart.html");
            await Task.WhenAll(putTask, deleteTask);
        }

        private static async Task CreateSecretsForFunctionsContainer(Site site)
        {
            var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
            var vfsManager = new RemoteVfsManager($"{site.ScmUrl}vfs/", credentials, retryCount: 3);
            var secrets = new
            {
                masterKey = Util.GetRandomHexNumber(40),
                functionKey = Util.GetRandomHexNumber(40)
            };
            await vfsManager.Put("data/functions/secrets/host.json", new StringContent(JsonConvert.SerializeObject(secrets)));
        }
    }
}