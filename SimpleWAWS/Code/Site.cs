using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Management.WebSites.Models;

namespace SimpleWAWS.Code
{
    public class Site
    {
        private WebSpace _webSpace;
        private WebSite _webSite;
        private WebSiteGetConfigurationResponse _config;

        private const string IsSimpleWAWSKey = "SIMPLE_WAWS";
        private const string InUseMetadataKey = "IN_USE";

        public Site(WebSpace webSpace, WebSite webSite)
        {
            _webSpace = webSpace;
            _webSite = webSite;
        }

        public Site(WebSite webSite, WebSiteGetConfigurationResponse config)
        {
            _webSite = webSite;
            _config = config;
        }

        public async Task LoadConfigurationAsync()
        {
            _config = await _webSpace.GetConfigurationAsync(Name);
        }

        public async Task InitializeNewSite()
        {
            var updateParams = Util.CreateWebSiteUpdateConfigurationParameters();

            // Mark it as our site
            updateParams.Metadata = new Dictionary<string, string> {
                {IsSimpleWAWSKey, "1"}
            };

            // Turn on Monaco
            updateParams.AppSettings = new Dictionary<string, string> {
                {"WEBSITE_NODE_DEFAULT_VERSION", "0.10.21"},
                {"MONACO_EXTENSION_VERSION", "beta"}
            };

            await _webSpace.UpdateConfigurationAsync(Name, updateParams);
            Trace.TraceInformation("Updated initial config for site '{0}' in {1}", this, _webSpace);

            // Get all the configuration
            _config = await _webSpace.GetConfigurationAsync(Name);
            Trace.TraceInformation("Read the configuration for site '{0}' in {1}", this, _webSpace);
        }

        public string Name { get { return _webSite.Name; } }

        // We use the password as an ID so users can't access other users's sites
        public string Id { get { return PublishingPassword; } }

        public bool IsSimpleWAWS
        {
            get
            {
                return _config.Metadata.ContainsKey(IsSimpleWAWSKey);
            }
        }

        public bool IsInUse
        {
            get
            {
                return _config.Metadata.ContainsKey(InUseMetadataKey);
            }
        }

        public string Url
        {
            get {
                return String.Format("http://{0}/", _webSite.HostNames[0]);
            }
        }

        public string ScmUrl
        {
            get
            {
                string scmHostName = _webSite.EnabledHostNames.First(n => n.Contains(".scm."));
                return String.Format("https://{0}/", scmHostName);
            }
        }

        public string ScmUrlWithCreds
        {
            get
            {
                string scmHostName = _webSite.EnabledHostNames.First(n => n.Contains(".scm."));
                return String.Format("https://{0}:{1}@{2}/", PublishingUserName, PublishingPassword, scmHostName);
            }
        }

        public string KuduConsoleWithCreds
        {
            get
            {
                return ScmUrlWithCreds + "DebugConsole";
            }
        }

        public string GitUrlWithCreds
        {
            get
            {
                return ScmUrlWithCreds + Name + ".git";
            }
        }

        public string MonacoUrl
        {
            get
            {
                return ScmUrlWithCreds + "dev";
            }
        }

        public string ContentDownloadUrl
        {
            get
            {
                return ScmUrlWithCreds + "zip/site/wwwroot";
            }
        }

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

        public DateTime StartTime { get { return _webSite.LastModifiedTimeUtc; } }
        public string PublishingUserName { get { return _config.PublishingUserName; } }
        public string PublishingPassword { get { return _config.PublishingPassword; } }

        public Task DeleteAndCreateReplacementAsync()
        {
            return _webSpace.DeleteAndCreateReplacementAsync(this);
        }

        public async Task MarkAsInUseAsync()
        {
            _webSite.LastModifiedTimeUtc = DateTime.UtcNow;

            var updateParams = Util.CreateWebSiteUpdateConfigurationParameters();
            _config.Metadata[InUseMetadataKey] = "true";
            updateParams.Metadata = _config.Metadata;

            await _webSpace.UpdateConfigurationAsync(Name, updateParams);
        }

        public override string ToString()
        {
            return Name;
        }

    }
}
