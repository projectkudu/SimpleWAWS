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
        public static string WebsiteNodeDefautlVersion { get { return config("6.9.4"); } }
        public static string AppInsightsInstrumentationKey { get { return config(); } }
        public static string GraphUserName { get { return config(); } }
        public static string GraphPassword { get { return config(); } }
        public static string ZippedRepoUrl { get { return config(); } }
        public static int CleanupExpiredResourceGroupsMinutes { get { return Int32.Parse(config("5")); } }
        public static int CleanupSubscriptionFirstInvokeMinutes { get { return Int32.Parse(config("5")); } }
        public static int CleanupSubscriptionMinutes { get { return Int32.Parse(config("15")); } }
        public static int LoqQueueStatsMinutes { get { return Int32.Parse(config("5")); } }
        public static int BackGroundQueueSize{ get { return Int32.Parse(config("50")); } }
        public static string LinuxTenantId { get { return config(TryTenantId); } }
        public static string LinuxTenantName { get { return config(TryTenantName); } }
        public static string LinuxServicePrincipal { get { return config(TryUserName); } }
        public static string LinuxServicePrincipalKey { get { return config(TryPassword); } }
        public static string LinuxExpiryMinutes { get { return config("30"); } }
        public static string LinuxSubscriptions { get { return config(); } }
        public static string LinuxGeoRegions { get { return config("West US,Southeast Asia"); } }
        public static int LinuxResourceGroupsPerRegion { get { return Int32.Parse(config("1")); } }
        public static string VSCodeLinuxTenantId { get { return config(); } }
        public static string VSCodeLinuxTenantName { get { return config(); } }
        public static string VSCodeLinuxServicePrincipal { get { return config(); } }
        public static string VSCodeLinuxServicePrincipalKey { get { return config(); } }
        public static string VSCodeLinuxExpiryMinutes { get { return config("60"); } }
        public static string VSCodeLinuxSubscriptions { get { return config(); } }
        public static string VSCodeLinuxGeoRegions { get { return config("South Central US,North Europe"); } }
        public static int VSCodeLinuxResourceGroupsPerTemplate { get { return Int32.Parse(config("4")); } }
        public static string MonitoringToolsExpiryMinutes { get { return config("240"); } }
        public static string MonitoringToolsTenantName { get { return config(); } }
        public static string MonitoringToolsTenantId { get { return config(); } }
        public static string MonitoringToolsServicePrincipal { get { return config(); } }
        public static string MonitoringToolsServicePrincipalKey { get { return config(); } }
        public static string MonitoringToolsResourceGroupName { get { return config("musicstore-trial-test"); } }
        public static string MonitoringToolsSubscription { get { return config("0396d5df-005a-4c6b-9665-714ae60c5eb9"); } }
        public static string MonitoringToolsGraphUserName { get { return config(); } }
        public static string MonitoringToolsGraphPassword { get { return config(); } }
        public static string WEBSITE_SLOT_NAME { get { return config(); } }
        public static string WEBSITE_HOSTNAME { get { return config();}}
        public static int NUMBER_OF_PROCESSORS { get { return Int32.Parse(config("4")); } }
        public static string ServerFarmTypeContent { get { return config(); } }
    }

}
   