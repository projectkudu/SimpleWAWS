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

        private static IEnumerable<WebsiteTemplate> _templatesList;

        static TemplatesManager()
        {
            try
            {
                _templatesList = new List<WebsiteTemplate>();
                var list = _templatesList as List<WebsiteTemplate>;
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
                            Language = language,
                            SpriteName = string.Format("{0} {1}", iconCssClass, cssClass),
                            AppService =  language.Equals("Mobile", StringComparison.OrdinalIgnoreCase) ? AppService.Mobile : AppService.Web
                        });
                    }
                }
                //TODO: Implement a FileSystemWatcher for changes in the directory
            }
            catch (Exception)
            {
                _templatesList = Enumerable.Empty<WebsiteTemplate>();
            }
        }

        public static IEnumerable<WebsiteTemplate> GetTemplates()
        {
            return _templatesList;
        }
    }
    public static class TemplatesExtensions
    {
        public static string GetFullPath(this WebsiteTemplate value)
        {
            return Path.Combine(TemplatesManager.TemplatesFolder, value.Language, value.FileName);
        }
    }
}