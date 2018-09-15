using SimpleWAWS.Code;
using System;
using System.Linq;

namespace SimpleWAWS.Models
{
    public abstract class BaseResource
    {
        public string SubscriptionId { get; protected set; }
        public SubscriptionType SubscriptionType
        {
            get
            {
                return SimpleSettings.MonitoringToolsSubscription.Equals(SubscriptionId, StringComparison.OrdinalIgnoreCase) ? SubscriptionType.MonitoringTools :
                       SimpleSettings.LinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                       .Contains(SubscriptionId)
                       ? SubscriptionType.Linux :
                       SimpleSettings.VSCodeLinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                       .Contains(SubscriptionId)
                       ? SubscriptionType.VSCodeLinux
                       : SubscriptionType.AppService;
            }
        }
        public string TenantName
        {
            get
            {
                return SimpleSettings.MonitoringToolsSubscription.Equals(SubscriptionId, StringComparison.OrdinalIgnoreCase)
                       ? SimpleSettings.MonitoringToolsTenantName
                       : SimpleSettings.LinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(SubscriptionId)
                       ? SimpleSettings.LinuxTenantName
                       : SimpleSettings.TryTenantName;
            }
        }
        public string TenantId
        {
            get
            {
                return SimpleSettings.MonitoringToolsSubscription.Equals(SubscriptionId, StringComparison.OrdinalIgnoreCase)
                       ? SimpleSettings.MonitoringToolsTenantId
                       : SimpleSettings.LinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(SubscriptionId)
                       ? SimpleSettings.LinuxTenantId
                       : SimpleSettings.TryTenantId;
            }
        }

        public string ResourceGroupName { get; protected set; }

        public abstract string CsmId { get; }
        public string TemplateName { get; set; }

        public BaseResource(string subscriptionId, string resourceGroupName)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
        }
        public BaseResource(string subscriptionId, string resourceGroupName,string templateName)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
            this.TemplateName = templateName;
        }

    }
}