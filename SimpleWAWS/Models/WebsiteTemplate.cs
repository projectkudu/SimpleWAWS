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
        public string FileName { get; set; }

        public string Language { get; set; }

        public static WebsiteTemplate EmptySiteTemplate
        {
            get { return new WebsiteTemplate() { Name = "Empty Site", Language = "Empty Site", SpriteName = "sprite-Large" }; }
        }
    }
}