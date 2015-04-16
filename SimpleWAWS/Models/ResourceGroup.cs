using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class ResourceGroup : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}";

        public override string CsmId
        {
            get
            {
                return string.Format(_csmIdTemplate, SubscriptionId, ResourceGroupName);
            }
        }

        public string UserId
        {
            get { return Tags.ContainsKey(Constants.UserId) ? Tags[Constants.UserId] : null; }
        }

        public DateTime StartTime 
        {
            get { return DateTime.Parse(Tags[Constants.StartTime]); }
        }

        public string ResourceUniqueId 
        {
            get { return ResourceGroupName.Split('_').LastOrDefault(); }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public AppService AppService { get; set; }

        public IEnumerable<Site> Sites { get; set; }

        public IEnumerable<ApiApp> ApiApps { get; set; }

        public string GeoRegion 
        {
            get { return Tags[Constants.GeoRegion]; }
        }

        public bool IsRbacEnabled
        {
            get { return bool.Parse(Tags[Constants.IsRbacEnabled]); }
            set { Tags[Constants.IsRbacEnabled] = value.ToString(); }
        }

        public Dictionary<string, string> Tags { get; set; }

        public bool IsSimpleWAWS
        {
            get
            {
                return !string.IsNullOrEmpty(ResourceGroupName) && ResourceGroupName.StartsWith("TRY-AZURE-RG-");
            }
        }

        public ResourceGroup(string subsciptionId, string resourceGroupName)
        {
            this.SubscriptionId = subsciptionId;
            this.ResourceGroupName = resourceGroupName;
            Sites = Enumerable.Empty<Site>();
            Tags = new Dictionary<string, string>();
        }
    }
}