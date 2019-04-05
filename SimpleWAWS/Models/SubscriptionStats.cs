using System;
using System.Collections.Generic;

namespace SimpleWAWS.Models
{

    public class SubscriptionStats
    {
        public IEnumerable<ResourceGroup> Ready { get; set; }
        public IEnumerable<ResourceGroup> ToDelete { get; set; }
    }
}