using System.Globalization;

namespace SimpleWAWS.Models
{
    public class Gateway : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Appservice/gateways/{2}";
        public string GatewayName { get; set; }

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _csmIdTemplate, SubscriptionId, ResourceGroupName, GatewayName);
            }
        }

        public Gateway(string subscriptionId, string resourceGroupName, string gatewayName)
            : base(subscriptionId, resourceGroupName)
        {
            this.GatewayName = gatewayName;
        }
    }
}