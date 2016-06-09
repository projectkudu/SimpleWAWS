using System;
using System.Collections.Generic;
using System.Linq;


namespace SimpleWAWS.Models.CsmModels
{
    public class CsmSubscriptionsArray
    {
        public IEnumerable<CsmSubscription> value { get; set; }
    }
}