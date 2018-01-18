using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SimpleWAWS.Code;
using System.Globalization;

namespace SimpleWAWS.Models
{
    public class Site : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/sites/{2}";

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _csmIdTemplate, SubscriptionId, ResourceGroupName, SiteName);
            }
        }

        public string SiteName { get; private set; }

        public Dictionary<string, string> AppSettings { get; set; }

        public Dictionary<string, string> Metadata { get; set; }

        public string HostName { get; set; }

        public string ScmHostName { get; set; }

        public Site(string subscriptionId, string resourceGroupName, string name, string kind = null)
            : base (subscriptionId, resourceGroupName)
        {
            this.SiteName = name;
            this.Kind = kind;
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
                return ScmUrlWithCreds + "basicauth";
            }
        }

        public string ContentDownloadUrl
        {
            get
            {
                return string.Concat("https://tryappservice.azure.com/api/resource/getwebappcontent/", SiteName);
            }
        }

        public string IbizaUrl
        {
            get
            {
                return string.Concat("https://portal.azure.com/", TenantName, "#resource", CsmId);
            }
        }

        public string PublishingUserName { get; set; }

        public string PublishingPassword { get; set; }

        public bool IsSimpleWAWSOriginalSite
        {
            get
            {
                return !string.IsNullOrEmpty(SiteName) &&
                       Regex.IsMatch(SiteName, "^[A-F0-9]{8}-0ee0-4-231-b9ee$", RegexOptions.IgnoreCase);
            }
        }
        public string Kind { get; set; }

        public bool IsFunctionsContainer
        {
            get
            {
                return !string.IsNullOrEmpty(Kind) &&
                    Kind.StartsWith(Constants.FunctionsContainerSiteKind);
            }
        }
    }
}
