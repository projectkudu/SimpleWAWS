using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SimpleWAWS.Code;

namespace SimpleWAWS.Controllers
{
    public class HomeController : Controller
    {
        private const string WAWSSiteCookie = "WAWSSite";
        private const string IdCookieValue = "Id";

        public async Task<ActionResult> Index()
        {
            return View();
        }
    }
}