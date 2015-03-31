using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class ResourceGroup
    {
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

        public string SubscriptionId { get; private set; }

        public string ResourceGroupName { get; private set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AppService AppService { get; set; }

        public IEnumerable<Site> Sites { get; set; }

        public string GeoRegion 
        {
            get { return Tags[Constants.GeoRegion]; }
        }

        public bool IsRbacEnabled
        {
            get { return bool.Parse(Tags[Constants.IsRbacEnabled]); }
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