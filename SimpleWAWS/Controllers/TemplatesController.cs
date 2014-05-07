using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using SimpleWAWS.Code;

namespace SimpleWAWS.Controllers
{
    public class TemplatesController : ApiController
    {
        public IEnumerable<Template> Get()
        {
            return TemplatesManager.GetTemplates();
        }
    }
}