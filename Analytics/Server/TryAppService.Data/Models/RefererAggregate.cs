using System;
using Newtonsoft.Json;

namespace TryAppService.Data.Models
{
    public class RefererAggregate
    {
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "referer", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Path { get; set; }

        [JsonProperty(PropertyName = "count", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "hour", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime Hour { get; set; }
    }
}