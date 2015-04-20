using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class ServerFarm : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/serverFarms/{2}";
        public string ServerFarmName { get; private set; }

        public override string CsmId
        {
            get
            {
                return string.Format(this._csmIdTemplate, this.SubscriptionId, this.ResourceGroupName, this.ServerFarmName);
            }
        }

        public ServerFarm(string subscriptionId, string resoruceGroupName, string serverFarmName)
            : base(subscriptionId, resoruceGroupName)
        {
            this.ServerFarmName = serverFarmName;
        }
    }
}