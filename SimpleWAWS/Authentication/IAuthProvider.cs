using System.Web;

namespace SimpleWAWS.Authentication
{
    public interface IAuthProvider
    {
        void AuthenticateRequest(HttpContext context);
        bool HasToken(HttpContext context);
    }
}