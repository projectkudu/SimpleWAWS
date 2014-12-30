using System.Configuration;
using System.Web;
using SimpleWAWS.Authentication;
using System.Collections.Generic;

namespace SimpleWAWS.Code
{
    public static class SecurityManager
    {
        private static readonly Dictionary<string, IAuthProvider> _authProviders = new Dictionary<string,IAuthProvider>();

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
            _authProviders.Add("aad", new AADProvider()); // Constants.DefaultAuthProvider
            _authProviders.Add("facebook", new FacebookAuthProvider()); 
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