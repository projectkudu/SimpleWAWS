using System.Configuration;
using System.Web;
using SimpleWAWS.Authentication;

namespace SimpleWAWS.Code
{
    public static class SecurityManager
    {
        private static IAuthProvider _authProvider;

        public static void SetAuthProvider(IAuthProvider authProvider)
        {
            _authProvider = authProvider;
        }

        public static void AuthenticateRequest(HttpContext context)
        {
            _authProvider.AuthenticateRequest(context);
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