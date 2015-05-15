using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public class ApiApp : BaseResource
    {
        public ApiApp(string subscriptionId, string resourceGroupName, string apiAppName)
            : base(subscriptionId, resourceGroupName)
        {
            this.ApiAppName = apiAppName;
        }

        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.AppService/apiapps/{2}";

        public override string CsmId
        {
            get
            {
                return string.Format(_csmIdTemplate, SubscriptionId, ResourceGroupName, ApiAppName);
            }
        }

        public string MicroserviceId { get; set; }

        public string Location { get; set; }

        public string ApiAppName { get; private set; }

        public JObject GeneratePayload()
        {
            return JObject.FromObject(new
            {
                microserviceId = MicroserviceId,
                hostingPlan = new
                {
                    subscriptionId = SubscriptionId,
                    resourceGroup = ResourceGroupName,
                    hostingPlanName = "Default1",
                    isNewHostingPlan = false,
                    computeMode = "Shared",
                    siteMode = "Limited",
                    sku = "Free",
                    workerSize = 0,
                    location = Location
                },
                settings = new { },
                dependsOn = new object[] { }
            });
        }

        public JObject GenerateTemplateParameters()
        {
            return JObject.FromObject(new Dictionary<string, object> 
            {
                { "location", new { value = Location } },
                { MicroserviceId,  new { value = new Dictionary<string, string> { { "$apiAppName", ApiAppName } } } }
            });
        }

        public string IbizaUrl
        {
            get
            {
                return string.Concat("https://portal.azure.com/", SimpleSettings.TryTenantName, "#resource", CsmId);
            }
        }
    }
}