using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;

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
                        var iconUri = Path.Combine(ImagesFolder, string.Format("{0}.png", Path.GetFileNameWithoutExtension(template)));
                        var cssClass = Path.GetFileNameWithoutExtension(template).Replace(" ", "").Replace("#", "Sharp");
                        var iconCssClass = File.Exists(iconUri) ? string.Format("sprite-{0}", cssClass) : "sprite-Large";
                        var language = Path.GetFileName(languagePath);
                        list.Add(new WebsiteTemplate
                        {
                            Name = Path.GetFileNameWithoutExtension(template),
                            FileName = Path.GetFileName(template),
                            Language = language.Equals("Mobile", StringComparison.OrdinalIgnoreCase) ? null : language,
                            SpriteName = string.Format("{0} {1}", iconCssClass, cssClass),
                            AppService =  language.Equals("Mobile", StringComparison.OrdinalIgnoreCase) ? AppService.Mobile : AppService.Web
                        });
                    }
                }
                list.Add(new ApiTemplate
                    {
                        Name = "TrySamplesTodoList",
                        SpriteName = "sprite-APIApps",
                        AppService = AppService.Api
                    });
                list.Add(new ApiTemplate
                    {
                        Name = "TrySamplesContactList",
                        SpriteName = "sprite-APIApps",
                        AppService = AppService.Api
                    });
                //TODO: Implement a FileSystemWatcher for changes in the directory
            }
            catch (Exception)
            {
                _templatesList = Enumerable.Empty<BaseTemplate>();
            }
        }

        public static IEnumerable<BaseTemplate> GetTemplates()
        {
            return _templatesList;
        }
    }
    public static class TemplatesExtensions
    {
        public static string GetFullPath(this WebsiteTemplate value)
        {
            var language = value.Language ?? "Mobile";
            return Path.Combine(TemplatesManager.TemplatesFolder, language, value.FileName);
        }
    }
}