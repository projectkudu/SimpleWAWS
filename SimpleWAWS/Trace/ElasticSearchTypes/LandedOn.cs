using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Trace.ElasticSearchTypes
{
    public class LandedOn
    {
        [JsonProperty(PropertyName = "app_service")]
        public string AppService { get; set; }

        [JsonProperty(PropertyName = "template")]
        public string Template { get; set; }

        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }
    }
}