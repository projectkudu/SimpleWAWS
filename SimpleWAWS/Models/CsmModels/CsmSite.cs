using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{

    public class CsmSite
    {
        public string[] hostNames { get; set; }
        public string[] enabledHostNames { get; set; }
    }
}