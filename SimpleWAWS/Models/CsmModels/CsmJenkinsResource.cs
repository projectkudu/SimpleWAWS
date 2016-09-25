using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmJenkinsResource
    {
        public Dictionary<string, string> dnsSettings { get; set; }
        public string ipAddress { get; set; }
    }
}