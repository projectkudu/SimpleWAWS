using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Globalization;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Code;
using SimpleWAWS.Resources;
namespace SimpleWAWS.Models
{
    public static class TemplatesManager
    {
        internal static string TemplatesFolder
        {
            get
            {
                var folder = HostingEnvironment.MapPath(@"~/App_Data/Templates");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        internal static string ImagesFolder
        {
            get
            {
                var folder = HostingEnvironment.MapPath(@"~/Content/images/packages");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        private static dynamic _baseARMTemplate;

        private static IEnumerable<BaseTemplate> _templatesList;

        static TemplatesManager()
        {
            try
            {
                _templatesList = new List<BaseTemplate>();
                var list = _templatesList as List<BaseTemplate>;
                foreach (var languagePath in Directory.GetDirectories(TemplatesFolder))
                {
                    foreach (var template in Directory.GetFiles(languagePath))
                    {
                        var iconUri = Path.Combine(ImagesFolder, string.Format(CultureInfo.InvariantCulture, "{0}.png", Path.GetFileNameWithoutExtension(template)));
                        var cssClass = GetShortName(Path.GetFileNameWithoutExtension(template));
                        var iconCssClass = File.Exists(iconUri) ? string.Format(CultureInfo.InvariantCulture, "sprite-{0}", cssClass) : "sprite-Large";
                        var language = Path.GetFileName(languagePath);
                        list.Add(new WebsiteTemplate
                        {
                            Name = Path.GetFileNameWithoutExtension(template),
                            FileName = Path.GetFileName(template),
                            Language = (language.Equals("Mobile", StringComparison.OrdinalIgnoreCase) || language.Equals("Api", StringComparison.OrdinalIgnoreCase)) ? null : language,
                            SpriteName = string.Format(CultureInfo.InvariantCulture, "{0} {1}", iconCssClass, cssClass),
                            AppService = language.Equals("Mobile", StringComparison.OrdinalIgnoreCase) ? AppService.Mobile : language.Equals("Api", StringComparison.OrdinalIgnoreCase) ? AppService.Api : AppService.Web,
                            MSDeployPackageUrl = $"{SimpleSettings.ZippedRepoUrl}/{Uri.EscapeDataString(Path.GetFileName(Path.GetDirectoryName(template)))}/{Uri.EscapeDataString(Path.GetFileName(template))}"
                        });
                    }
                }
                list.Add(new LogicTemplate
                {
                    Name = "Ping Site",
                    SpriteName = "sprite-PingSite PingSite",
                    AppService = AppService.Logic,
                    CsmTemplateFilePath = HostingEnvironment.MapPath("~/ARMTemplates/PingSite.json"),
                    Description = Resources.Server.Templates_PingSiteDescription
                });
                list.Add(new JenkinsTemplate
                {
                    Name = "Jenkins CI",
                    SpriteName = "sprite-PingSite PingSite",
                    AppService = AppService.Jenkins,
                    CsmTemplateFilePath = HostingEnvironment.MapPath("~/ARMTemplates/JenkinsResource.json"),
                    Description = Resources.Server.Templates_JenkinsDescription
                });
                _baseARMTemplate = JObject.Parse( File.ReadAllText(HostingEnvironment.MapPath("~/ARMTemplates/BaseARMTemplate.json")));
                //TODO: Implement a FileSystemWatcher for changes in the directory
            }
            catch (Exception)
            {
                _templatesList = Enumerable.Empty<BaseTemplate>();
            }
        }

        private static string GetShortName(string templateName)
        {
            return templateName.Replace(" ", "").Replace(".", "").Replace("+", "");
        }

        public static IEnumerable<BaseTemplate> GetTemplates()
        {
            return _templatesList;
        }

        public static JObject GetARMTemplate(BaseTemplate template)
        {
            var armTemplate = JObject.FromObject(_baseARMTemplate);
            updateParameters( armTemplate, template as WebsiteTemplate);
            updateAppSettings(armTemplate, template as WebsiteTemplate);
            updateConfig(armTemplate, template as WebsiteTemplate);
            return armTemplate;
        }
        private static void updateParameters(dynamic temp, WebsiteTemplate template)
        {
            var shortName = GetShortName(template.Name);
            temp.parameters.appServiceName.defaultValue = $"{Server.ARMTemplate_MyPrefix}-{shortName}{Server.ARMTemplate_AppPostfix}-{Guid.NewGuid().ToString().Split('-')[0]}";
            temp.parameters.msdeployPackageUrl.defaultValue = template.MSDeployPackageUrl;
        }

        private static void updateConfig(dynamic armTemplate, WebsiteTemplate template)
        {
            if (template.GithubRepo == null)
            {
                if (template.Name.Equals("WordPress", StringComparison.OrdinalIgnoreCase))
                {
                    armTemplate.resources[1].resources[0].properties.scmType = "LocalGit";
                    armTemplate.resources[1].resources[0].properties.httpLoggingEnabled = true;
                    armTemplate.resources[1].resources[0].properties.localMySqlEnabled = true;
                }
                else
                {
                    armTemplate.resources[1].resources[0].properties.scmType = "LocalGit";
                    armTemplate.resources[1].resources[0].properties.httpLoggingEnabled = true;
                }
            }
        }

        private static void updateAppSettings(dynamic armTemplate, WebsiteTemplate template)
        {
            if (((template.Language != null) && template.Language.Equals("NodeJs", StringComparison.OrdinalIgnoreCase)) || template.Name.Equals("ExpressJs", StringComparison.OrdinalIgnoreCase) || template.Name.Equals("Express", StringComparison.OrdinalIgnoreCase))
            {
                armTemplate.resources[1].properties.siteConfig.appSettings.Add(new JObject {
                { "name","WEBSITE_NODE_DEFAULT_VERSION" },
                { "value","5.8.0" },
              });
            }
        }
    }
    public static class TemplatesExtensions
    {
        public static string GetFullPath(this WebsiteTemplate value)
        {
            var language = value.Language ?? ((value.AppService == AppService.Api) ? "Api" : "Mobile");
            return Path.Combine(TemplatesManager.TemplatesFolder, language, value.FileName);
        }
    }
}