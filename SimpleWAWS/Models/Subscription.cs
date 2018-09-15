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
        VSCodeLinux,
        MonitoringTools
    }

    public class Subscription
    {
        public string SubscriptionId { get; private set; }

        public SubscriptionType Type
        {
            get
            {
                return SimpleSettings.MonitoringToolsSubscription.Equals(SubscriptionId,StringComparison.OrdinalIgnoreCase)?SubscriptionType.MonitoringTools: 
                       SimpleSettings.LinuxSubscriptions.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                       .Contains(SubscriptionId)
                       ? SubscriptionType.Linux:
                       SimpleSettings.VSCodeLinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                       .Contains(SubscriptionId)
                       ? SubscriptionType.VSCodeLinux
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
                    case SubscriptionType.VSCodeLinux:
                        return SimpleSettings.VSCodeLinuxGeoRegions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());
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
                    default:
                        return 0;
                };
            }
        }
        public int ResourceGroupsPerTemplate
        {
            get
            {
                switch (Type)
                {
                    case SubscriptionType.VSCodeLinux:
                        return SimpleSettings.VSCodeLinuxResourceGroupsPerTemplate;
                    default:
                        return 0;
                };
            }
        }
    }
}