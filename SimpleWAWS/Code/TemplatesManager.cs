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

        private static string TemplatesDefinition
        {
            get { return Path.Combine(TemplatesFolder, "templates.json"); }
        }

        private static IEnumerable<Template> _templatesList;

        static TemplatesManager()
        {
            try
            {
                var jsonFile = File.ReadAllText(TemplatesDefinition);
                _templatesList = JsonConvert.DeserializeObject<Template[]>(jsonFile);
                //TODO: Implement a FileSystemWatcher for changes in templates.json
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
            return Path.Combine(TemplatesManager.TemplatesFolder, value.FileName);
        }
    }
}