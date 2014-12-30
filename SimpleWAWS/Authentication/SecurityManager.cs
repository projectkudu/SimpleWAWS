using System.Configuration;
using System.Web;
using SimpleWAWS.Authentication;
using System.Collections.Generic;
using System;

namespace SimpleWAWS.Code
{
    public static class SecurityManager
    {
        private static readonly Dictionary<string, IAuthProvider> _authProviders =
            new Dictionary<string, IAuthProvider>(StringComparer.InvariantCultureIgnoreCase);

        private static IAuthProvider GetAuthProvider(HttpContext context)
        {
            var requestedAuthProvider = string.IsNullOrEmpty(string.Empty)
                                        ? Constants.DefaultAuthProvider
                                        : string.Empty;

            IAuthProvider authProvider;
            if (_authProviders.TryGetValue(requestedAuthProvider, out authProvider))
            {
                return authProvider;
            }
            else
            {
                return _authProviders[Constants.DefaultAuthProvider];
            }
        }

        public static void InitAuthProviders()
        {
            _authProviders.Add("AAD", new AADProvider());
            _authProviders.Add("Facebook", new FacebookAuthProvider());
            _authProviders.Add("Twitter", new TwitterAuthProvider());
            _authProviders.Add("Google", new GoogleAuthProvider());
        }

        public static void AuthenticateRequest(HttpContext context)
        {
            GetAuthProvider(context).AuthenticateRequest(context);
        }

        public static bool HasToken(HttpContext context)
        {
            return GetAuthProvider(context).HasToken(context);
        }

        public static void EnsureAdmin(HttpContext context)
        {
            if (context.User.Identity.Name != ConfigurationManager.AppSettings["AdminUserId"])
            {
                context.Response.StatusCode = 403; //Forbidden
                context.Response.End();
            }
        }
    }
}