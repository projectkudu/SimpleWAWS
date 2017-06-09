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
            list.ForEach(t =>
            {
                if (t.AppService == AppService.Api)
                {
                    t.Description = Resources.Server.Templates_APIAppDescription;
                }
                if (t.AppService == AppService.Logic)
                {
                    t.Description = Resources.Server.Templates_PingSiteDescription;
                }
                else if (t.AppService == AppService.Mobile)
                {
                    switch (t.Name)
                    {
                        case "Todo List":
                            t.Description = Resources.Server.Templates_TodoListDescription;
                            break;
                        case "Xamarin CRM":
                            t.Description = Resources.Server.Templates_XamarinCrmDescription;
                            break;
                        case "Field Engineer":
                            t.Description = Resources.Server.Templates_FieldEngineerDescription;
                            break;
                    }
                }
            });
            list.Add(WebsiteTemplate.EmptySiteTemplate);
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