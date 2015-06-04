using System.Web;

namespace SimpleWAWS.Authentication
{
    public interface IAuthProvider
    {
        void AuthenticateRequest(HttpContextBase context);
        bool HasToken(HttpContextBase context);
    }
}