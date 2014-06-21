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

        public static void HandleCallBack(HttpContext context)
        {
            _authProvider.HandleCallBack(context);
        }
    }
}