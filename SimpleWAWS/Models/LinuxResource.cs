using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SimpleWAWS.Models
{
    public class LinuxResource : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites{2}";
        public string SiteName { get; private set; }
        public string HostName { get; set; }
        public string ScmHostName { get; set; }

        public LinuxResource(string subscriptionId, string resourceGroupName, string name, string kind = null)
            : base(subscriptionId, resourceGroupName)
        {
            this.SiteName = name;
            this.Kind = kind;
        }
        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _csmIdTemplate, SubscriptionId, ResourceGroupName, SiteName);
            }
        }
        public string Kind { get; set; }

        public string Location { get; set; }

        public string IbizaUrl
        {
            get
            {
                return string.Concat("https://portal.azure.com/", SimpleSettings.TryTenantName, "#resource", CsmId);
            }
        }
        public string Url
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "https://{0}/", HostName);
            }
        }

        public string GetMobileUrl(string templateName)
        {
            return Url + (templateName.Equals("Todo List", StringComparison.OrdinalIgnoreCase) ? "jsclient" : "admin");
        }

        public string ScmUrl
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "https://{0}/", ScmHostName);
            }
        }

        public string ScmUrlWithCreds
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "https://{0}:{1}@{2}/", PublishingUserName, PublishingPassword, ScmHostName);
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
                return ScmUrlWithCreds + SiteName + ".git";
            }
        }

        public string MonacoUrl
        {
            get
            {
                return ScmUrl + "dev";
            }
        }

        public string ContentDownloadUrl
        {
            get
            {
                return ScmUrl + "zip/site/wwwroot";
            }
        }

        public string PublishingUserName { get; set; }

        public string PublishingPassword { get; set; }

    }
}