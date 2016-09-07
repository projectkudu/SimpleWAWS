using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class LogicApp : BaseResource
    {
        public LogicApp(string subscriptionId, string resourceGroupName, string logicAppName)
            : base(subscriptionId, resourceGroupName)
        {
            this.LogicAppName = logicAppName;
        }

        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Logic/workflows/{2}";

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _csmIdTemplate, SubscriptionId, ResourceGroupName, LogicAppName);
            }
        }

        public string LogicAppName { get; private set; }
        public string Location { get; set; }

        public string IbizaUrl
        {
            get
            {
                return string.Concat("https://portal.azure.com/", SimpleSettings.TryTenantName, "#resource", CsmId);
            }
        }
    }
}