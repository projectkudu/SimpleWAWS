using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using SimpleWAWS.Models;

namespace SimpleWAWS.Controllers
{
    public class TemplatesController : ApiController
    {
        public IEnumerable<BaseTemplate> Get()
        {
            var list = TemplatesManager.GetTemplates().ToList();
            list.ForEach(t => { if (t.AppService == AppService.Logic) t.Description = Resources.Server.Templates_PingSiteDescription; });
            list.Add(WebsiteTemplate.EmptySiteTemplate);
            return list;
        }
    }
}