using System;
using System.Collections.Generic;
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

        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.AppService/logicapps/{2}";

        public override string CsmId
        {
            get
            {
                return string.Format(_csmIdTemplate, SubscriptionId, ResourceGroupName, LogicAppName);
            }
        }

        public string LogicAppName { get; set; }
        public string Location { get; set; }
    }
}