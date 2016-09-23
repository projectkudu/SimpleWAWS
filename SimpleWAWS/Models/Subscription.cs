using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SimpleWAWS.Code;

namespace SimpleWAWS.Models
{
    public enum SubscriptionType
    {
        AppService,
        Jenkins
    }

    public class Subscription
    {
        public string SubscriptionId { get; private set; }
        public SubscriptionType Type {
            get
            {
                return
                    (SimpleSettings.JenkinsSubscriptions.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .Contains(SubscriptionId))
                        ? SubscriptionType.Jenkins
                        : SubscriptionType.AppService;

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