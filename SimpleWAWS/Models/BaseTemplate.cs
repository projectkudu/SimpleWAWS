using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class BaseTemplate
    {
        [JsonProperty(PropertyName="name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName="sprite")]
        public string SpriteName { get; set; }

        [JsonProperty(PropertyName="appService")]
        [JsonConverter(typeof(StringEnumConverter))]
        public AppService AppService { get; set; }
    }
}