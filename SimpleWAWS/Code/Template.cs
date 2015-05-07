﻿using System;
using System.Collections.Generic;
using System.EnterpriseServices.Internal;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        [JsonProperty("icon_class")]
        public string IconClass { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("appService")]
        public AppService AppService { get; set; }

        public static Template EmptySiteTemplate
        {
            get { return new Template() { Name = "Empty Site", Language = "Empty Site", IconClass = "sprite-Large" }; }
        }
    }
}