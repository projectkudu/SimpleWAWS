using Newtonsoft.Json;

namespace SimpleWAWS.Models
{
    public class WebsiteTemplate : BaseTemplate
    {
        public static BaseTemplate EmptySiteTemplate
        {
            get { return new BaseTemplate() { Name = "Empty Site", Language = "Default", SpriteName = "sprite-Large" }; }
        }
        public static BaseTemplate DefaultTemplate(string templateName, AppService appService, string language,
    string filename, string dockerContainer, string msdeployPackageUrl)
        {
            return new BaseTemplate()
            {
                Name = templateName,
                AppService = appService,
                Language = language,
                FileName = filename,
                DockerContainer = dockerContainer,
                MSDeployPackageUrl = msdeployPackageUrl
            };
        }
    }
}