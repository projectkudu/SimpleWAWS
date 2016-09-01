using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmWrapper<T>
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string kind { get; set; }
        public string location { get; set; }
        public Dictionary<string, string> sku { get; set; }
        public Dictionary<string, string> tags { get; set; }
        public T properties { get; set; }
    }
}