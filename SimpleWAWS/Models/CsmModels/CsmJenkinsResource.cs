using System.Collections.Generic;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmJenkinsResource
    {
        public Dictionary<string, string> dnsSettings { get; set; }
        public string ipAddress { get; set; }
    }
}