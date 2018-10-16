using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using SimpleWAWS.Models;
using System.Net.Http;
using System.Net;
using System;
using System.IO;
using System.Net.Http.Headers;
using SimpleWAWS.Code;

namespace SimpleWAWS.Controllers
{
    public class TemplatesController : ApiController
    {
        public IEnumerable<BaseTemplate> Get()
        {
            var list = TemplatesManager.GetTemplates().ToList();
            return list;
        }
        public HttpResponseMessage GetARMTemplate(string templateName)
        {
            var list = TemplatesManager.GetTemplates().ToList();
            var emptyTemplate= WebsiteTemplate.EmptySiteTemplate;
            emptyTemplate.MSDeployPackageUrl = $"{SimpleSettings.ZippedRepoUrl}/Default/{Uri.EscapeDataString((emptyTemplate.Name))}.zip";

            list.Add(emptyTemplate);

            var template = list.FirstOrDefault((temp) => string.Equals(temp.Name, templateName,StringComparison.OrdinalIgnoreCase ));
            if (template != null)
            {
                var armTemplateJson = TemplatesManager.GetARMTemplate(template);
                return Request.CreateResponse(HttpStatusCode.OK, armTemplateJson, new MediaTypeHeaderValue("application/json"));
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }
    }
}