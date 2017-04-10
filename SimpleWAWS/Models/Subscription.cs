using System;
using System.Collections.Generic;
using System.Linq;
using SimpleWAWS.Code;

namespace SimpleWAWS.Models
{
    public enum SubscriptionType
    {
        AppService,
        Jenkins,
        Linux
    }

    public class Subscription
    {
        public string SubscriptionId { get; private set; }

        public SubscriptionType Type
        {
            get
            {
                return
                    (SimpleSettings.JenkinsSubscriptions.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .Contains(SubscriptionId))
                        ? SubscriptionType.Jenkins
                        : SimpleSettings.LinuxSubscriptions.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                            .Contains(SubscriptionId)
                            ? SubscriptionType.Linux
                            : SubscriptionType.AppService;
            }
        }

        public IEnumerable<ResourceGroup> ResourceGroups { get; set; }

        public Subscription(string subscriptionId)
        {
            this.SubscriptionId = subscriptionId;
            ResourceGroups = Enumerable.Empty<ResourceGroup>();
        }
        public IEnumerable<string> GeoRegions
        {
            get
            {
                switch (Type)
                {
                    case SubscriptionType.AppService:
                        return SimpleSettings.GeoRegions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());
                    case SubscriptionType.Linux:
                        return SimpleSettings.LinuxGeoRegions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());
                    case SubscriptionType.Jenkins:
                        return SimpleSettings.JenkinsGeoRegions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());
                    default:
                        return null;
                };
            }
        }

        public int ResourceGroupsPerGeoRegion
        {
            get
            {
                switch (Type)
                {
                    case SubscriptionType.AppService:
                        return 1;
                    case SubscriptionType.Linux:
                        return SimpleSettings.LinuxResourceGroupsPerRegion;
                    case SubscriptionType.Jenkins:
                        return SimpleSettings.JenkinsResourceGroupsPerRegion;
                    default:
                        return 0;
                };
            }
        }
    }
}