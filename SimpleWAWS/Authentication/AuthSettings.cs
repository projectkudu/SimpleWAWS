using System.Configuration;
using System.Runtime.CompilerServices;

namespace SimpleWAWS.Authentication
{
    public static class AuthSettings
    {
        private static string config(string @default = null, [CallerMemberName] string key = null)
        {
            var value = System.Environment.GetEnvironmentVariable(key) ?? ConfigurationManager.AppSettings[key];
            return string.IsNullOrEmpty(value)
                ? @default
                : value;
        }
        public static string AADTokenEndpoint { get { return config("https://login.windows-ppe.net/common/oauth2/token"); } }
        public static string AADAppId { get { return config("2f2b272a-e0a7-458b-88d8-83c09ae715e5"); } }

        public static string AADAppCertificateThumbprint { get { return config(); } }

        public static string EnableAuth { get { return config(); } }
        public static string BaseLoginUrl { get { return config(); } }
        public static string LoginErrorPage { get { return config(); } }
        public static string FacebookAppId { get { return config(); } }
        public static string GoogleAppId { get { return config(); } }
        public static string AADIssuerKeys { get { return config(); } }
        public static string GoogleIssuerCerts { get { return config(); } }
        public static string AdminUserId { get { return config(); } }
        public static string AdminUserKeys { get { return config(); } }
        public static string VkClientSecret { get { return config(); } }
        public static string VkClientId { get { return config(); } }
        public static string GitHubClientSecret { get { return config(); } }
        public static string GitHubClientId { get { return config(); } }
    }
}