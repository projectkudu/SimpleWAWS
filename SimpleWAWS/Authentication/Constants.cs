using System;

namespace SimpleWAWS.Authentication
{
    public static class Constants
    {
        public const string EncryptionReason = "ProtectCookie";
        public const string LoginSessionCookie = "loginsession";
        public const string BaseLoginUrl = "BaseLoginUrl";
        public const string AADAppId = "AADAppId";
        public static readonly TimeSpan SessionCookieValidTimeSpan = TimeSpan.FromHours(8);
        public const string BearerHeader = "Bearer ";
        public const string AADIssuerKeys = "AADIssuerKeys";
        public const string GoogleIssuerCerts = "GoogleIssuerCerts";
        public const string DefaultAuthProvider = "Google";
    }
}