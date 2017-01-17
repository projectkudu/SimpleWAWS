using System;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Web.Hosting;

namespace SimpleWAWS.Code
{
    public static class SimpleSettings
    {
        private static string config(string @default = null, [CallerMemberName] string key = null)
        {
            var value = System.Environment.GetEnvironmentVariable(key) ?? ConfigurationManager.AppSettings[key];
            return string.IsNullOrEmpty(value)
                ? @default
                : value;
        }

        public static string TryTenantId { get { return config(); } }
        public static string TryTenantName { get { return config(); } }
        public static string SiteExpiryMinutes { get { return config("60"); } }
        public static string GeoRegions { get { return config("East US,West US,North Europe,West Europe,South Central US,North Central US,East Asia,Southeast Asia,Japan West,Japan East,Brazil South"); } }
        public static string TryUserName { get { return config(); } }
        public static string TryPassword { get { return config(); } }
        public static string Subscriptions { get { return config(); } }
        public static string FreeSitesIISLogsBlob { get { return config(); } }
        public static string FreeSitesIISLogsQueue { get { return config(); } }
        public static string StorageConnectionString { get { return config(); } }
        public static string DocumentDbUrl { get { return config(); } }
        public static string DocumentDbKey { get { return config(); } }
        public static string FromEmail { get { return config(); } }
        public static string EmailServer { get { return config(); } }
        public static string EmailUserName { get { return config(); } }
        public static string EmailPassword { get { return config(); } }
        public static string ToEmails  { get { return config(); } }
        public static string SearchServiceName { get { return config(); } }
        public static string SearchServiceApiKeys { get { return config(); } }
        public static string ExtendedResourceExpireHours { get { return config("24"); } }
        private const string CommonApiAppsCsmTemplatePathLocal = @"C:\Users\fashaikh\Documents\GitHub\SimpleWAWS\SimpleWAWS\CSMTemplates\commonApiApps.json";
        public static string CommonApiAppsCsmTemplatePath { get; } = HostingEnvironment.MapPath("~/CSMTemplates/commonApiApps.json") ?? CommonApiAppsCsmTemplatePathLocal;
        public static string ElasticSearchUri = "http://10.0.0.4:9200";
        public static string FunctionsExtensionVersion { get { return config("latest"); } }
        public static string AzureJobsExtensionVersion { get { return config("beta"); } }
        public static string MonacoExtensionVersion { get { return config("beta"); } }
        public static string WebsiteNodeDefautlVersion { get { return config("6.4.0"); } }
        public static string JenkinsTenant { get { return config(); } }
        public static string JenkinsServicePrincipal { get { return config(); } }
        public static string JenkinsServicePrincipalKey { get { return config(); } }
        public static string JenkinsVMPassword { get { return config(); } }
        public static string JenkinsExpiryMinutes { get { return config(); } }
        public static string JenkinsSubscriptions { get { return config(); } }
        public static string JenkinsGeoRegions { get { return config(); } }
        public static int JenkinsResourceGroupsPerRegion { get { return Int32.Parse(config()); } }
        public static string AppInsightsInstrumentationKey { get { return config(); } }

        public static string GraphUserName { get { return config(); } }
        public static string GraphPassword { get { return config(); } }

    }
}   