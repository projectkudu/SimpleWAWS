using Newtonsoft.Json;
using System;

namespace TryAppService.Data.Models
{
    public class UIEvent
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty(PropertyName="userName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UserName { get; set; }

        [JsonProperty(PropertyName="dateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime DateTime { get; set; }

        [JsonProperty(PropertyName="eventName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string EventName { get; set; }

        [JsonProperty(PropertyName = "experiment", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Experiment { get; set; }

        [JsonProperty(PropertyName = "properties", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Properties { get; set; }

        [JsonProperty(PropertyName = "sourceVariation", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SourceVariation { get; set; }

        [JsonProperty(PropertyName = "anonymousUserName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AnonymousUserName { get; set; }

        [JsonProperty(PropertyName = "userCulture", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UserCulture { get; set; }
    }
}