using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class MakeJenkinsSubscriptionFreeTrialResult
    {
        public IEnumerable<ResourceGroup> Ready { get; set; }
        public IEnumerable<Tuple<string, int>> ToCreateInRegions { get; set; }
        public IEnumerable<ResourceGroup> ToDelete{ get; set; }
    }
}