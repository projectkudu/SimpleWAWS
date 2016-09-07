using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using SimpleWAWS.Code;
using SimpleWAWS.Models.CsmModels;

namespace SimpleWAWS.Models
{
    public class ServerFarm : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverFarms/{2}";
        public string ServerFarmName { get; private set; }
        public Sku Sku { get; private set; }
        public string Location { get; private set; }

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, this._csmIdTemplate, this.SubscriptionId, this.ResourceGroupName, this.ServerFarmName);
            }
        }

        public ServerFarm(string subscriptionId, string resourceGroupName, string name, string location)
            : base(subscriptionId, resourceGroupName)
        {
            this.ServerFarmName = name;
            this.Location = location;
            var sku = new Sku
            {
                capacity = Constants.TryAppServiceSkuCapacity,
                family = Constants.TryAppServiceSkuFamily,
                name = Constants.TryAppServiceSkuName,
                size = Constants.TryAppServiceSkuName,
                tier = Constants.TryAppServiceTier
            };
            this.Sku = sku;
        }
    }
}