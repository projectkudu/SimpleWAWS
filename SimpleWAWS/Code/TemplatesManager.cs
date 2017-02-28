using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Globalization;
using Newtonsoft.Json.Linq;

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
                        var cssClass = getShortName(Path.GetFileNameWithoutExtension(template));
                        var iconCssClass = File.Exists(iconUri) ? string.Format(CultureInfo.InvariantCulture, "sprite-{0}", cssClass) : "sprite-Large";
                        var language = Path.GetFileName(languagePath);
                        list.Add(new WebsiteTemplate
                        {
                            Name = Path.GetFileNameWithoutExtension(template),
                            FileName = Path.GetFileName(template),
                            Language = (language.Equals("Mobile", StringComparison.OrdinalIgnoreCase) || language.Equals("Api", StringComparison.OrdinalIgnoreCase)) ? null : language,
                            SpriteName = string.Format(CultureInfo.InvariantCulture, "{0} {1}", iconCssClass, cssClass),
                            AppService = language.Equals("Mobile", StringComparison.OrdinalIgnoreCase) ? AppService.Mobile : language.Equals("Api", StringComparison.OrdinalIgnoreCase) ? AppService.Api : AppService.Web,
                            MSDeployPackageUrl = $"https://github.com/fashaikh/appservice-zipped-templates/raw/master/{Path.GetDirectoryName(template)}/{Path.GetFileName(template)}"
                        });
                    }
                }
                list.Add(new LogicTemplate
                {
                    Name = "Ping Site",
                    SpriteName = "sprite-PingSite PingSite",
                    AppService = AppService.Logic,
                    CsmTemplateFilePath = HostingEnvironment.MapPath("~/ARMTemplates/PingSite.json"),
                    Description = Resources.Server.Templates_PingSiteDescription,
                    MSDeployPackageUrl = $"https://github.com/fashaikh/appservice-zipped-templates/raw/master/Logic/Ping%20Site.zip"
                });
                list.Add(new JenkinsTemplate
                {
                    Name = "Jenkins CI",
                    SpriteName = "sprite-PingSite PingSite",
                    AppService = AppService.Jenkins,
                    CsmTemplateFilePath = HostingEnvironment.MapPath("~/ARMTemplates/JenkinsResource.json"),
                    Description = Resources.Server.Templates_JenkinsDescription
                });
                _baseARMTemplate = JObject.Parse(File.ReadAllText(HostingEnvironment.MapPath("~/ARMTemplates/BaseARMTemplate.json"),  System.Text.Encoding.ASCII)
                    .Replace(Environment.NewLine, String.Empty)
                    .Replace(@"\", String.Empty));
                //TODO: Implement a FileSystemWatcher for changes in the directory
            }
            catch (Exception ex)
            {
                _templatesList = Enumerable.Empty<BaseTemplate>();
            }
        }

        private static object getShortName(string templateName)
        {
            return templateName.Replace(" ", "").Replace("#", "Sharp").Replace(".", "").Replace("+", "");
        }

        public static IEnumerable<BaseTemplate> GetTemplates()
        {
            return _templatesList;
        }
        public static JObject GetARMTemplate(BaseTemplate template)
        {
            var armTemplate = _baseARMTemplate;
            updateParameters(ref armTemplate, template);
            //TODO: Add mroe specific app settings as needed.
            updateAppSettings(ref armTemplate, template);
            updateConfig(ref armTemplate, template);
            return armTemplate;
        }
        private static void updateParameters(ref dynamic temp, BaseTemplate template)
        {
            var shortName = getShortName(template.Name);
            temp.parameters.appServiceName.defaultValue = string.Concat("My-", shortName, "App-", Guid.NewGuid().ToString().Split('-')[0]);
            temp.parameters.msdeployPackageUrl.defaultValue = template.MSDeployPackageUrl;
        }

        private static void updateConfig(ref dynamic temp, BaseTemplate template)
        {
            temp.resources[1].resources[0].properties.templateName = template.Name ;
        }

        private static void updateAppSettings(ref dynamic temp, BaseTemplate template)
        {
            temp.resources[1].properties.siteConfig.appSettings.Add(new JObject {
                { "name","foo"+ template.Name },
                { "value","bar" },
              });
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