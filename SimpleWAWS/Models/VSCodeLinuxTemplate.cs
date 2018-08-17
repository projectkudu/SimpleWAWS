using Newtonsoft.Json;
using System.Linq;

namespace SimpleWAWS.Models
{
    public class VSCodeLinuxTemplate : BaseTemplate
    {

        public static VSCodeLinuxTemplate GetVSCodeLinuxTemplate(string templateName)
        {
            return (VSCodeLinuxTemplate)TemplatesManager.GetTemplates()?.FirstOrDefault(t => (t.AppService == AppService.VSCodeLinux && t.Name == templateName) );
        }

    }
}