using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using SimpleWAWS.Code;

namespace SimpleWAWS.Models
{
    public class ServerFarm : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverFarms/{2}";
        public string ServerFarmName { get; private set; }
        public Dictionary<string, string> Sku { get; private set; }
        public string Location { get; private set; }

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, this._csmIdTemplate, this.SubscriptionId, this.ResourceGroupName, this.ServerFarmName);
            }
        }

        public ServerFarm(string subscriptionId, string resourceGroupName, string name, string location, 
            string sku = Constants.TryAppServiceSku,
            string skuname= Constants.TryAppServiceSkuName, 
            string skusize= Constants.TryAppServiceSkuName, 
            string skufamily= Constants.TryAppServiceSkuFamily,
            int skucapacity=Constants.TryAppServiceSkuCapacity)
            : base(subscriptionId, resourceGroupName)
        {
            this.ServerFarmName = name;
            this.Location = location;
            this.Sku = new Dictionary<string, string> {["tier"] = sku, ["name"]= skuname, ["size"] = skusize,
                ["family"] = skufamily, ["capacity"] = skucapacity.ToString()};
        }
    }
}