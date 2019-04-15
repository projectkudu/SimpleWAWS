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
                return TemplatesManager.GetSubscriptionTypeList()[SubscriptionId];
            }
        }
        public string TenantName
        {
            get
            {
                return SimpleSettings.TrySPTenantName;
            }
        }
        public string ResourceGroupName { get; protected set; }

        public abstract string CsmId { get; }
        public virtual string TemplateName { get; set; }

        public BaseResource(string subscriptionId, string resourceGroupName)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
        }
    }
}