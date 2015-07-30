using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.Hosting;

namespace SimpleWAWS.Code
{
    public static class SimpleSettings
    {
        private static string config([CallerMemberName] string key = null)
        {
            return System.Environment.GetEnvironmentVariable(key) ??
                ConfigurationManager.AppSettings[key];
        }

        public static string TryTenantId { get { return config(); } }
        public static string TryTenantName { get { return config(); } }
        public static string SiteExpiryMinutes { get { return config(); } }
        public static string GeoRegions { get { return config(); } }
        public static string GraphAndCsmUserName { get { return config(); } }
        public static string GraphAndCsmPassword { get { return config(); } }
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
        private const string CommonApiAppsCsmTemplatePathLocal = @"D:\scratch\repos\SimpleWAWS\SimpleWAWS\CSMTemplates\commonApiApps.json";
        public static string CommonApiAppsCsmTemplatePath { get; } = HostingEnvironment.MapPath("~/CSMTemplates/commonApiApps.json") ?? CommonApiAppsCsmTemplatePathLocal;
    }
}