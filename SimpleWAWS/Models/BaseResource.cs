namespace SimpleWAWS.Models
{
    public abstract class BaseResource
    {
        public string SubscriptionId { get; protected set; }

        public string ResourceGroupName { get; protected set; }

        public abstract string CsmId { get; }

        public BaseResource(string subscriptionId, string resourceGroupName)
        {
            this.SubscriptionId = subscriptionId;
            this.ResourceGroupName = resourceGroupName;
        }
    }
}