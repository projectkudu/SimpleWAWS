﻿using Newtonsoft.Json;
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

        [JsonProperty(PropertyName = "githubRepo")]
        public string GithubRepo { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        public string CreateQueryString()
        {
            return string.Concat("appServiceName=", AppService.ToString(), "&name=", Name, "&autoCreate=true");
        }
    }
}