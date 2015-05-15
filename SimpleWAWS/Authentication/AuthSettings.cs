using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public static class AuthSettings
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

        public static string BaseLoginUrl { get { return config(GetCurrentProperty()); } }
        public static string AADAppId { get { return config(GetCurrentProperty()); } }
        public static string LoginErrorPage { get { return config(GetCurrentProperty()); } }
        public static string FacebookAppId { get { return config(GetCurrentProperty()); } }
        public static string GoogleAppId { get { return config(GetCurrentProperty()); } }
        public static string AADIssuerKeys { get { return config(GetCurrentProperty()); } }
        public static string GoogleIssuerCerts { get { return config(GetCurrentProperty()); } }
        public static string AdminUserId { get { return config(GetCurrentProperty()); } }
    }
}