using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SimpleWAWS.Code;

namespace SimpleWAWS.Controllers
{
    public class LoginController : ApiController 
    {
        public HttpResponseMessage Login()
        {
            return SecurityManager.HandleLogin(HttpContext.Current);
        }
    }
}