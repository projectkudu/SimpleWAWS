using System;
using Newtonsoft.Json;

namespace TryAppService.Data.Models
{
    public class RequestsAggregate
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "path", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "hits", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Hits { get; set; }

        [JsonProperty(PropertyName = "hour", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime Hour { get; set; }
    }
}