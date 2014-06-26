using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Newtonsoft.Json;

namespace SimpleWAWS.Code
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

        private static IEnumerable<Template> _templatesList;

        static TemplatesManager()
        {
            try
            {
                _templatesList = new List<Template>();
                var list = _templatesList as List<Template>;
                foreach (var languagePath in Directory.GetDirectories(TemplatesFolder))
                {
                    foreach (var template in Directory.GetFiles(languagePath))
                    {
                        var iconUri = Path.Combine(ImagesFolder, string.Format("{0}.png", Path.GetFileNameWithoutExtension(template)));
                        list.Add(new Template
                        {
                            Name = Path.GetFileNameWithoutExtension(template),
                            FileName = Path.GetFileName(template),
                            Language = Path.GetFileName(languagePath),
                            IconUrl = File.Exists(iconUri) ? new Uri(string.Format("/Content/images/packages/{0}.png", Uri.EscapeDataString(Path.GetFileNameWithoutExtension(template))), UriKind.Relative) : new Uri("/Content/images/Large.png", UriKind.Relative)
                        });
                    }
                }
                //TODO: Implement a FileSystemWatcher for changes in the directory
            }
            catch (Exception)
            {
                _templatesList = Enumerable.Empty<Template>();
            }
        }

        public static IEnumerable<Template> GetTemplates()
        {
            return _templatesList;
        }
    }
    public static class TemplatesExtensions
    {
        public static string GetFullPath(this Template value)
        {
            return Path.Combine(TemplatesManager.TemplatesFolder, value.Language, value.FileName);
        }
    }
}