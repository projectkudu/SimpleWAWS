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
        public static int CleanupSubscriptionMinutes { get { return Int32.Parse(config("15")); } }
        public static int LoqQueueStatsMinutes { get { return Int32.Parse(config("5")); } }
        public static int BackGroundQueueSize{ get { return Int32.Parse(config("50")); } }
        public static string LinuxTenant { get { return config(TryTenantId); } }
        public static string LinuxServicePrincipal { get { return config(TryUserName); } }
        public static string LinuxServicePrincipalKey { get { return config(TryPassword); } }
        public static string LinuxExpiryMinutes { get { return config("30"); } }
        public static string LinuxSubscriptions { get { return config("594c2e22-3815-4a51-aa53-2fe0a3b5c1dc,0d8bca30-4765-42e9-b9c6-4f4278c380b2,18387d60-d7f0-4641-8611-a4c0447d7f85,20fa64d9-5434-4861-bfbc-36512109e1bb,ec284601-1580-4324-92bc-af5dd0af904e"); } }
        public static string LinuxGeoRegions { get { return config("West US,West Europe,Southeast Asia"); } }
        public static int LinuxResourceGroupsPerRegion { get { return Int32.Parse(config("10")); } }

    }
}   