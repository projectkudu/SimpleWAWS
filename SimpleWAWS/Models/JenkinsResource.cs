using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class JenkinsResource : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/.Network/publicIPAddresses/TrialJenkinsVMPublicIP";
        private string _ipAddress = String.Empty;
        public JenkinsResource(string subscriptionId, string resourceGroupName, string ipAddress)
            : base(subscriptionId, resourceGroupName)
        {
            this._ipAddress = ipAddress;
        }

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _csmIdTemplate, SubscriptionId, ResourceGroupName);
            }
        }

        public string JenkinsResourceUrl
        {
            get { return String.Format($"http://{_ipAddress}:8080"); }
            set { }
        }
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