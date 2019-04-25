using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Globalization;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Code;
using SimpleWAWS.Resources;
using Newtonsoft.Json;
using System.Net;
using SimpleWAWS.Trace;

namespace SimpleWAWS.Models
{
    public static class TemplatesManager
    {
        private static dynamic _baseARMTemplate;

        private static IEnumerable<BaseTemplate> _templatesList;
        private static Dictionary<string, SubscriptionType> _subTypeList;

        static TemplatesManager()
        {
            try
            {
                _templatesList = new List<BaseTemplate>();
                var list = _templatesList as List<BaseTemplate>;
                List<BaseTemplate> templates = JsonConvert.DeserializeObject<List<BaseTemplate>>( GetConfig("templates.json"));
                List <Config> config = JsonConvert.DeserializeObject<List<Config>>(GetConfig("config.json"));
                foreach (var template in templates)
                {
                    var configToUse = config.First(a => a.AppService == template.AppService.ToString());
                    template.Config = configToUse;
                }
                list.AddRange(templates);
                _subTypeList = new Dictionary<string, SubscriptionType>();
                foreach (var template in GetTemplates())
                {
                    foreach (var sub in template.Config.Subscriptions)
                    {
                        if (!_subTypeList.ContainsKey(sub))
                        {
                            _subTypeList.Add(sub, template.Config.SubscriptionType);
                        }
                    }
                }
                //Use JObject.Parse to quickly build up the armtemplate object used for LRS
                _baseARMTemplate = JObject.Parse(GetConfig("BaseARMTemplate.json"));
                //TODO: Implement a FileSystemWatcher for changes in the directory
            }
            catch (Exception)
            {
                _templatesList = Enumerable.Empty<BaseTemplate>();
            }
        }

        private static string GetConfig(string configjson)
        {
            var s = String.Empty;
            try
            {
                using (WebClient client = new WebClient())
                {
                    // Add a user agent header in case the 
                    // requested URI contains a query.

                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                    using (Stream data = client.OpenRead(SimpleSettings.ConfigUrl + configjson))
                    {
                        using (StreamReader reader = new StreamReader(data))
                        {
                            s = reader.ReadToEnd();
                            reader.Close();
                        }
                        data.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleTrace.TraceException(ex);
            }
            return s;     
         }

        private static string GetShortName(string templateName)
        {
            return templateName.Replace(" ", "").Replace(".", "").Replace("+", "");
        }

        public static IEnumerable<BaseTemplate> GetTemplates()
        {
            return _templatesList;
        }
        public static AppService GetAppService(string templateName)
        {
            return _templatesList.First(a=> a.Name==templateName).AppService;
        }
        public static Dictionary<string, SubscriptionType> GetSubscriptionTypeList()
        {
            return _subTypeList;
        }

        public static JObject GetARMTemplate(BaseTemplate template)
        {
            //Using JObject.FromObject to deep clone the base ARM template for modifications 
            //based on the requested website. Its needed since we dont want to make changes 
            //to the main _baseARMTemplate. This is also thread safe.
            var armTemplate = JObject.FromObject(_baseARMTemplate);
            UpdateParameters(armTemplate, template as WebsiteTemplate);
            UpdateConfig(armTemplate, template as WebsiteTemplate);
            return armTemplate;
        }
        private static void UpdateParameters(dynamic temp, WebsiteTemplate template)
        {
            var shortName = GetShortName(template.Name);
            temp.parameters.appServiceName.defaultValue = $"{Server.ARMTemplate_MyPrefix}-{shortName}{Server.ARMTemplate_AppPostfix}-{Guid.NewGuid().ToString().Split('-')[0]}";
            temp.parameters.msdeployPackageUrl.defaultValue = template.MSDeployPackageUrl;
        }

        private static void UpdateConfig(dynamic armTemplate, WebsiteTemplate template)
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

        private static void UpdateAppSettings(dynamic armTemplate, WebsiteTemplate template)
        {
        }
    }

}