using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class MakeSubscriptionFreeTrialResult
    {
        public IEnumerable<ResourceGroup> Ready { get; set; }
        public IEnumerable<string> ToCreateInRegions { get; set; }
        public IEnumerable<ResourceGroup> ToDelete{ get; set; }
    }
}