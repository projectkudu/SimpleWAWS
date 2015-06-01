using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class SimpleSettings
    {
        private static string config(string key)
        {
            return System.Environment.GetEnvironmentVariable(key) ??
                ConfigurationManager.AppSettings[key];
        }

        private static string GetCurrentProperty([CallerMemberName] string name = null)
        {
            return name;
        }

        public static string TryTenantId { get { return config(GetCurrentProperty()); } }
        public static string TryTenantName { get { return config(GetCurrentProperty()); } }
        public static string SiteExpiryMinutes { get { return config(GetCurrentProperty()); } }
        public static string GeoRegions { get { return config(GetCurrentProperty()); } }
        public static string GraphAndCsmUserName { get { return config(GetCurrentProperty()); } }
        public static string GraphAndCsmPassword { get { return config(GetCurrentProperty()); } }
        public static string TryUserName { get { return config(GetCurrentProperty()); } }
        public static string TryPassword { get { return config(GetCurrentProperty()); } }
        public static string Subscriptions { get { return config(GetCurrentProperty()); } }
        public static string FreeSitesIISLogsBlob { get { return config(GetCurrentProperty()); } }
        public static string FreeSitesIISLogsQueue { get { return config(GetCurrentProperty()); } }
        public static string StorageConnectionString { get { return config(GetCurrentProperty()); } }
        public static string DocumentDbUrl { get { return config(GetCurrentProperty()); } }
        public static string DocumentDbKey { get { return config(GetCurrentProperty()); } }
        public static string FromEmail { get { return config(GetCurrentProperty()); } }
        public static string EmailServer { get { return config(GetCurrentProperty()); } }
        public static string EmailUserName { get { return config(GetCurrentProperty()); } }
        public static string EmailPassword { get { return config(GetCurrentProperty()); } }
        public static string ToEmails  { get { return config(GetCurrentProperty()); } }
        public static string SearchServiceName { get { return config(GetCurrentProperty()); } }
        public static string SearchServiceApiKeys { get { return config(GetCurrentProperty()); } }

    }
}