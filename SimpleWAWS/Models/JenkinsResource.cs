using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SimpleWAWS.Models
{
    public class JenkinsResource : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Network/publicIPAddresses/TrialJenkinsVMPublicIP";
        private string _ipAddress = String.Empty;
        private string _hostName = String.Empty;
        public JenkinsResource(string subscriptionId, string resourceGroupName, string ipAddress, Dictionary<string, string> dnsSettings)
            : base(subscriptionId, resourceGroupName)
        {
            this._ipAddress = ipAddress;
            this._hostName = dnsSettings?["fqdn"];
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
            get { return string.IsNullOrEmpty(_ipAddress)? string.Empty: string.Format($"http://{_ipAddress}:8080"); }
        }
        public string JenkinsDnsUrl
        {
            get { return string.IsNullOrEmpty(_hostName) ? string.Empty : string.Format($"http://{_hostName}:8080"); }
        }
        public string Location { get; set; }

        public string IbizaUrl
        {
            get
            {
                return string.Concat("https://portal.azure.com/", SimpleSettings.JenkinsTenant, "#resource", CsmId);
            }
        }
    }
}