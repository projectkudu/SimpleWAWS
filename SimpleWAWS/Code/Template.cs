using System;
using System.Collections.Generic;
using System.EnterpriseServices.Internal;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Newtonsoft.Json;

namespace SimpleWAWS.Code
{
    public class Template
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("fileName")]
        public string FileName { get; set; }
        [JsonProperty("language")]
        public string Language { get; set; }
    }
}