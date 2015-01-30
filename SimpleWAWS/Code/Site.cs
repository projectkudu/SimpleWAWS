using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using Kudu.Client.Editor;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using Newtonsoft.Json;
using SimpleWAWS.Kudu;

namespace SimpleWAWS.Code
{
    public class Site
    {
        private WebSite _webSite;
        private WebSiteGetConfigurationResponse _config;
        private WebSiteGetPublishProfileResponse _publishingProfile;

        private const string IsSimpleWAWSKey = "SIMPLE_WAWS";
        private const string UserIdMetadataKey = "USERID";
        private const string SiteUniqueIdMetadataKey = "SITEUNIQUEID";

        [JsonIgnore]
        public WebSpace WebSpace { get; private set; }

        public Site(WebSpace webSpace, WebSite webSite)
        {
            WebSpace = webSpace;
            _webSite = webSite;
            if (_webSite.EnabledHostNames.Count < 2)
            {
                Trace.TraceError("Odd bug, we get an incomplete site object. see comment in LoadAndCreateSitesAsync()");
            }
        }

        public Site(WebSite webSite, WebSiteGetConfigurationResponse config)
        {
            _webSite = webSite;
            _config = config;
        }

        public async Task LoadConfigurationAsync()
        {
            _config = await WebSpace.GetConfigurationAsync(Name);

            // This is done to make sure every site we read has Monaco enabled.
            // Otherwise the users will hit a 404 when they browse to Monaco
            var updateConfig = false;
            if (!_config.AppSettings.ContainsKey("MONACO_EXTENSION_VERSION"))
            {
                _config.AppSettings.Add("MONACO_EXTENSION_VERSION", "beta");
                updateConfig = true;
            }

            if (!_config.AppSettings.ContainsKey("WEBSITE_TRY_MODE"))
            {
                _config.AppSettings.Add("WEBSITE_TRY_MODE", "1");
                updateConfig = true;
            }

            if (updateConfig)
            {
                var updateParams = Util.CreateWebSiteUpdateConfigurationParameters();
                updateParams.Metadata = _config.Metadata;
                updateParams.AppSettings = _config.AppSettings;
                await WebSpace.UpdateConfigurationAsync(Name, updateParams);
            }

            _publishingProfile = await WebSpace.GetPublishingProfile(Name);
        }

        public async Task InitializeNewSite()
        {
            var updateParams = Util.CreateWebSiteUpdateConfigurationParameters();

            // Mark it as our site
            updateParams.Metadata = new Dictionary<string, string> {
                {IsSimpleWAWSKey, "1"},
                {SiteUniqueIdMetadataKey, Guid.NewGuid().ToString()}
            };

            // Turn on Monaco
            updateParams.AppSettings = new Dictionary<string, string> {
                {"WEBSITE_NODE_DEFAULT_VERSION", "0.10.32"},
                {"MONACO_EXTENSION_VERSION", "beta"},
                {"WEBSITE_TRY_MODE", "1"}
            };

            // Mark site's ScmType as LocalGit so that cloning an empty repo gives back the site content
            updateParams.ScmType = "LocalGit";

            await WebSpace.UpdateConfigurationAsync(Name, updateParams);
            Trace.TraceInformation("Updated initial config for site '{0}' in {1}", this, WebSpace);

            // Get all the configuration
            await LoadConfigurationAsync();

            Trace.TraceInformation("Read the configuration for site '{0}' in {1}", this, WebSpace);
        }

        [JsonProperty("name")]
        public string Name { get { return _webSite.Name; } }

        [JsonProperty("isSimpleWAWS")]
        public bool IsSimpleWAWS
        {
            get
            {
                return Regex.IsMatch(Name, "^[A-F0-9]{8}-0ee0-4-231-b9ee$", RegexOptions.IgnoreCase);
            }
        }

        public string UserId
        {
            get
            {
                string userId = null;
                _config.Metadata.TryGetValue(UserIdMetadataKey, out userId);
                return userId;
            }
        }

        public string SiteUniqueId
        {
            get
            {
                string siteuniqueId = null;
                _config.Metadata.TryGetValue(SiteUniqueIdMetadataKey, out siteuniqueId);
                return siteuniqueId;
            }
        }


        [JsonProperty("url")]
        public string Url
        {
            get {
                return String.Format("http://{0}/", _webSite.HostNames[0]);
            }
        }

        [JsonProperty("scmUrl")]
        public string ScmUrl
        {
            get
            {
                string scmHostName = _webSite.EnabledHostNames.First(n => n.Contains(".scm."));
                return String.Format("https://{0}/", scmHostName);
            }
        }

        [JsonProperty("scmUrlWithCreds")]
        public string ScmUrlWithCreds
        {
            get
            {
                string scmHostName = _webSite.EnabledHostNames.First(n => n.Contains(".scm."));
                return String.Format("https://{0}:{1}@{2}/", PublishingUserName, PublishingPassword, scmHostName);
            }
        }

        [JsonProperty("kuduConsoleWithCreds")]
        public string KuduConsoleWithCreds
        {
            get
            {
                return ScmUrlWithCreds + "DebugConsole";
            }
        }

        [JsonProperty("gitUrlWithCreds")]
        public string GitUrlWithCreds
        {
            get
            {
                return ScmUrlWithCreds + Name + ".git";
            }
        }

        [JsonProperty("monacoUrl")]
        public string MonacoUrl
        {
            get
            {
                return ScmUrl + "dev";
            }
        }

        [JsonProperty("contentDownloadUrl")]
        public string ContentDownloadUrl
        {
            get
            {
                return ScmUrl + "zip/site/wwwroot";
            }
        }

        [JsonProperty("timeLeftString")]
        public string TimeLeftString
        {
            get
            {
                TimeSpan timeUsed = DateTime.UtcNow - StartTime;
                TimeSpan timeLeft;
                if (timeUsed > SiteManager.SiteExpiryTime)
                {
                    timeLeft = TimeSpan.FromMinutes(0);
                }
                else
                {
                    timeLeft = SiteManager.SiteExpiryTime - timeUsed;
                }

                return String.Format("{0}m:{1:D2}s", timeLeft.Minutes, timeLeft.Seconds);
            }
        }

        [JsonProperty("startTime")]
        public DateTime StartTime { get { return _webSite.LastModifiedTimeUtc; } }

        [JsonProperty("publishingUrl")]
        public string PublishingUrl
        {
            get
            {
                return _publishingProfile.PublishProfiles.FirstOrDefault() == null
                    ? null
                    : _publishingProfile.PublishProfiles.FirstOrDefault().PublishUrl;
            }
        }

        [JsonProperty("publishingUserName")]
        public string PublishingUserName { get { return _config.PublishingUserName; } }

        [JsonProperty("publishingPassword")]
        public string PublishingPassword { get { return _config.PublishingPassword; } }

        public bool Ready { get { return _config != null && _config.Metadata != null && _webSite.EnabledHostNames.Count == 2; } }

        [JsonProperty("isRbacEnabled")]
        public bool IsRbacEnabled { get; set; }

        public Task DeleteAndCreateReplacementAsync()
        {
            return WebSpace.DeleteAndCreateReplacementAsync(this);
        }

        public async Task MarkAsInUseAsync(string userId, TimeSpan lifeTime)
        {
            _webSite.LastModifiedTimeUtc = DateTime.UtcNow;

            var updateParams = Util.CreateWebSiteUpdateConfigurationParameters();
            _config.Metadata[UserIdMetadataKey] = userId;
            _config.AppSettings["USER_ID"] = userId;
            _config.AppSettings["LAST_MODIFIED_TIME_UTC"] = DateTime.UtcNow.ToString();
            _config.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = lifeTime.TotalMinutes.ToString();
            updateParams.Metadata = _config.Metadata;
            updateParams.AppSettings = _config.AppSettings;

            // Turn on HttpLogging for analytics later on.
            updateParams.HttpLoggingEnabled = true;

            await WebSpace.UpdateConfigurationAsync(Name, updateParams);
        }

        public override string ToString()
        {
            return Name;
        }

        public Task<StreamContent> GetPublishingProfile()
        {
            var xmlList =
                _publishingProfile.PublishProfiles.Select(profile => profile.Serialize());
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<publishData>");
            foreach (var profile in xmlList)
            {
                stringBuilder.Append(profile);
            }
            stringBuilder.Append("</publishData>");

            return Task.FromResult(new StreamContent(stringBuilder.ToString().ToStream()));
        }

        public void FireAndForget()
        {
            try
            {
                var httpHeaders = "GET / HTTP/1.0\r\n" +
                "Host: " + this._webSite.HostNames[0] + "\r\n" +
                "\r\n";
                using (var tcpClient = new TcpClient(this._webSite.HostNames[0], 80))
                {
                    tcpClient.Client.Send(Encoding.ASCII.GetBytes(httpHeaders));
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                //log and ignore any tcp exceptions
                Trace.TraceWarning(ex.ToString());
            }
        }
    }
}
