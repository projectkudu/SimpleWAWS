using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public abstract class BaseTemplate
    {
        public string Name { get; set; }

        public string SpriteName { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AppService AppService { get; set; }
    }
}