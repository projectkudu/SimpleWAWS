using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class CsmTemplateWrapper
    {
        public CsmTemplateProperties properties { get; set; }
    }

    public class CsmTemplateProperties
    {
        public string mode { get; set; }

        public object parameters { get; set; }

        public JObject template { get; set; }
    }
}