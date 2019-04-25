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
        public static string TrySPTenantId { get { return config(); } }
        public static string TrySPTenantName { get { return config(); } }
        public static string TrySPUserName { get { return config(); } }
        public static string TrySPPassword { get { return config(); } }
        public static string SiteExpiryMinutes { get { return config("60"); } }
        public static string StorageConnectionString { get { return config(); } }
        public static string FromEmail { get { return config(); } }
        public static string EmailServer { get { return config(); } }
        public static string EmailUserName { get { return config(); } }
        public static string EmailPassword { get { return config(); } }
        public static string ToEmails  { get { return config(); } }
        public static string SearchServiceName { get { return config(); } }
        public static string SearchServiceApiKeys { get { return config(); } }
        public static string ExtendedResourceExpireHours { get { return config("24"); } }
        public static string FunctionsExtensionVersion { get { return config("~2"); } }
        public static string WebsiteNodeDefautlVersion { get { return config("6.9.4"); } }
        public static string FunctionsNodeDefaultVersion { get { return config("10.14.1"); } }
        public static string AppInsightsInstrumentationKey { get { return config(); } }
        public static string ConfigUrl { get { return config(); } }
        public static string TemplatesUrl { get { return config(); } }
        public static string ZippedRepoUrl { get { return config(); } }
        public static int CleanupExpiredResourceGroupsMinutes { get { return Int32.Parse(config("5")); } }
        public static int CleanupSubscriptionFirstInvokeMinutes { get { return Int32.Parse(config("5")); } }
        public static int CleanupSubscriptionMinutes { get { return Int32.Parse(config("15")); } }
        public static int LoqQueueStatsMinutes { get { return Int32.Parse(config("5")); } }
        public static int BackGroundQueueSize{ get { return Int32.Parse(config("50")); } }
        public static string WEBSITE_SLOT_NAME { get { return config(); } }
        public static string WEBSITE_HOSTNAME { get { return config(); } }
        public static string WEBSITE_HOME_STAMPNAME { get { return config("waws-prod-bay-035"); } }
        public static string BackgroundQueueManagerStampName { get { return config("waws-prod-bay-035"); } }
        public static string InUseResourceTableName { get { return config("inuseresources"); } }
        public static string PartitionKey { get { return config("A"); } }
        public static string FreeQueuePrefix{ get { return config("free-"); } }
        public static int MaxFreeSitesQueueLength { get { return Int32.Parse(config("30")); } }

        public static int NUMBER_OF_PROCESSORS { get { return Int32.Parse(config("4")); } }
        public static string ServerFarmTypeContent { get { return config(); } }
        public static string TryAppServiceSite {  get { return config("https://tryappservice.azure.com"); } }

    }

}
   