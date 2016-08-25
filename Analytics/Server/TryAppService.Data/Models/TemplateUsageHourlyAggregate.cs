using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace TryAppService.Data.Models
{
    public class TemplateUsageHourlyAggregate
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "language", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Language { get; set; }

        [JsonProperty(PropertyName = "total", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Total { get; set; }

        [JsonProperty(PropertyName = "hour", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime Hour { get; set; }
    }
}