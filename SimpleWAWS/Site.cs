using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;

namespace SimpleWAWS
{
    public class Site
    {
        private SiteManager _manager;
        private WebSite _webSite;
        private WebSiteGetConfigurationResponse _config;

        public Site(SiteManager manager, WebSite webSite, WebSiteGetConfigurationResponse config)
        {
            _manager = manager;
            _webSite = webSite;
            _config = config;
        }

        public string Name { get { return _webSite.Name; } }

        // We use the password as an ID so users can't access other users's sites
        public string Id { get { return PublishingPassword; } }

        public bool IsInUse
        {
            get
            {
                return _config.Metadata.ContainsKey(SiteManager.InUseMetadataKey);
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
                if (timeUsed > _manager.SiteExpiryTime)
                {
                    timeLeft = TimeSpan.FromMinutes(0);
                }
                else
                {
                    timeLeft = _manager.SiteExpiryTime - timeUsed;
                }

                return String.Format("{0}m:{1:D2}s", timeLeft.Minutes, timeLeft.Seconds);
            }
        }

        public DateTime StartTime { get { return _webSite.LastModifiedTimeUtc; } }
        public string PublishingUserName { get { return _config.PublishingUserName; } }
        public string PublishingPassword { get { return _config.PublishingPassword; } }

        public async Task MarkAsInUseAsync()
        {
            _webSite.LastModifiedTimeUtc = DateTime.UtcNow;

            var updateParams = Util.CreateWebSiteUpdateConfigurationParameters();
            updateParams.Metadata = new Dictionary<string, string> {
                {SiteManager.InUseMetadataKey, "true"}
            };

            await _manager.Client.WebSites.UpdateConfigurationAsync(_manager.WebSpaceName, Name, updateParams);
        }

        public override string ToString()
        {
            return Name;
        }

    }
}
