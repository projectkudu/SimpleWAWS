using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmDeployment
    {
        public string DeploymentName { get; set; }

        public string ResourceGroupName { get; set; }

        public string SubscriptionId { get; set; }

        public CsmTemplateWrapper CsmTemplate { get; set; }
    }
}