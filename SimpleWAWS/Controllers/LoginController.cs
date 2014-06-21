using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SimpleWAWS.Code;

namespace SimpleWAWS.Controllers
{
    public class LoginController : ApiController
    {
        public HttpResponseMessage Get(string id_token, string session_state)
        {
            if (SecurityManager.TryAuthenticateBarrer(HttpContext.Current, id_token))
            {
                var cookie = SecurityManager.CreateSessionCookie(HttpContext.Current.User);
                var response = new HttpResponseMessage(HttpStatusCode.Redirect);
                response.Headers.AddCookies(new[]
                {
                    cookie
                });
                
                response.Headers.Location = new Uri("/", UriKind.Relative);
                return response;
            }
            return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Error singing in");
        }
    }
}