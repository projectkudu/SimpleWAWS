using Newtonsoft.Json;
using System.Linq;

namespace SimpleWAWS.Models
{
    public class ContainersTemplate : BaseTemplate
    {
        public string CsmTemplateFilePath { get; set; }
        public static ContainersTemplate GetContainersTemplate(string templateName)
        {
            return (ContainersTemplate)TemplatesManager.GetTemplates()?.FirstOrDefault(t => (t.AppService == AppService.Containers) );
        }

    }
}