using Newtonsoft.Json;
using System.Linq;

namespace SimpleWAWS.Models
{
    public class LinuxTemplate : BaseTemplate
    {
        public string CsmTemplateFilePath { get; set; }
        public static LinuxTemplate GetLinuxTemplate(string templateName)
        {
            return (LinuxTemplate)TemplatesManager.GetTemplates()?.FirstOrDefault(t => (t.AppService == AppService.Web && t.Name == templateName) );
        }

    }
}