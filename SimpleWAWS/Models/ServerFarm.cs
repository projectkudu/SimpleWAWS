using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class ServerFarm : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverFarms/{2}";
        public string ServerFarmName { get; private set; }
        public Dictionary<string, string> Sku { get; set; }
        public string Location { get; set; }

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, this._csmIdTemplate, this.SubscriptionId, this.ResourceGroupName, this.ServerFarmName);
            }
        }

        public ServerFarm(string subscriptionId, string resourceGroupName, string serverFarmName, string location, string sku)
            : base(subscriptionId, resourceGroupName)
        {
            this.ServerFarmName = serverFarmName;
            this.Location = location;
            this.Sku["tier"] = sku;
        }
    }
}