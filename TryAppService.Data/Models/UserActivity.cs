using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace TryAppService.Data.Models
{
    public class UserActivity
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "userName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "dateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime DateTime { get; set; }

        [JsonProperty(PropertyName = "templateName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TemplateName { get; set; }

        [JsonProperty(PropertyName = "templateLanguage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TemplateLanguage { get; set; }

        [JsonProperty(PropertyName = "uniqueId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UniqueId { get; set; }

        [JsonProperty(PropertyName = "experiment", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Experiment { get; set; }

        [JsonProperty(PropertyName = "appService", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AppService { get; set; }

        [JsonProperty(PropertyName = "sourceVariation", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SourceVariation { get; set; }

        [JsonProperty(PropertyName = "anonymousUserName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AnonymousUserName { get; set; }
    }
}