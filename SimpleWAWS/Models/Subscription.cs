using System;
using System.Collections.Generic;
using System.Linq;
using SimpleWAWS.Code;

namespace SimpleWAWS.Models
{
    public enum SubscriptionType
    {
        AppService,
        Linux,
        VSCodeLinux
    }

    public class Subscription
    {
        public string SubscriptionId { get; private set; }

        public SubscriptionType Type
        {
            get
            {
                return SubscriptionType.AppService;
            }
        }

        public IEnumerable<ResourceGroup> ResourceGroups { get; set; }

        public Subscription(string subscriptionId)
        {
            this.SubscriptionId = subscriptionId;
            ResourceGroups = Enumerable.Empty<ResourceGroup>();
        }

    }
}