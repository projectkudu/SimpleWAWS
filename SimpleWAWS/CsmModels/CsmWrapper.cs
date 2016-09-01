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
        public Sku sku { get; set; }
        public Dictionary<string, string> tags { get; set; }
        public T properties { get; set; }
    }

    public class Sku
    {
        public string name { get; set; }
        public string tier { get; set; }
        public string size { get; set; }
        public string family { get; set; }
        public int capacity { get; set; }
    }
}