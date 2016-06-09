using System;
using System.Collections.Generic;
using System.EnterpriseServices.Internal;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SimpleWAWS.Models
{
    public class WebsiteTemplate : BaseTemplate
    {
        [JsonProperty(PropertyName="fileName")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName="language")]
        public string Language { get; set; }

        public static WebsiteTemplate EmptySiteTemplate
        {
            get { return new WebsiteTemplate() { Name = "Empty Site", Language = "Default", SpriteName = "sprite-Large" }; }
        }
    }
}