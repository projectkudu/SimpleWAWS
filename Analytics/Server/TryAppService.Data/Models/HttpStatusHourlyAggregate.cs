using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace TryAppService.Data.Models
{
    public class HttpStatusHourlyAggregate
    {
        [JsonIgnore]
        public int Id { get; set; }
        
        [JsonProperty(PropertyName = "statusCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int StatusCode { get; set; }

        [JsonProperty(PropertyName = "count", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Count { get; set; }
        
        [JsonProperty(PropertyName = "hour", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime Hour { get; set; }
    }
}