using System.Linq;

namespace SimpleWAWS.Models
{
    public class LinuxTemplate : BaseTemplate
    {
        public string DockerContainer { get; set; }
        public string CsmTemplateFilePath { get; set; }
        public static LinuxTemplate GetLinuxTemplate(string templateName)
        {
            return (LinuxTemplate)TemplatesManager.GetTemplates()?.FirstOrDefault(t => (t.AppService == AppService.Linux && t.Name == templateName) );
        }

    }
}