using System;

namespace SimpleWAWS.Authentication
{
    public static class AuthConstants
    {
        //public const string EncryptionReason = "ProtectCookie";
        public const string LoginSessionCookie = "loginsession";
        public static readonly TimeSpan SessionCookieValidTimeSpan = TimeSpan.FromMinutes(59);
        public const string BearerHeader = "Bearer ";
        public const string DefaultAuthProvider = "AAD";
        public const string AnonymousUser = "aus";
        public const string TiPCookie = "x-ms-routing-name";
    }
}