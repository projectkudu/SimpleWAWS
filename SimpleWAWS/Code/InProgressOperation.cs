using System;
using System.Threading;
using System.Threading.Tasks;
using SimpleWAWS.Models;
using Newtonsoft.Json;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Code.CsmExtensions;
using Newtonsoft.Json.Linq;

namespace SimpleWAWS.Code
{
    public class InProgressOperation
    {
        [JsonIgnore]
        public Task Task { get; private set;}

        public ResourceGroup ResourceGroup { get; private set; }

        public CsmDeployment Deployment { get; private set; }

        public DeploymentType DeploymentType { get; private set; }

        private readonly CancellationTokenSource _tokenSource;

        public InProgressOperation(ResourceGroup resourceGroup, DeploymentType deploymentType)
        {
            this.ResourceGroup = resourceGroup;
            this.DeploymentType = deploymentType;
            this._tokenSource = new CancellationTokenSource();
            this.Task = Task.Delay(Timeout.Infinite, this._tokenSource.Token);
        }

        public Task<JToken> CreateDeployment(object template, bool block)
        {
            Deployment = new CsmDeployment
            {
                DeploymentName = ResourceGroup.ResourceUniqueId,
                SubscriptionId = ResourceGroup.SubscriptionId,
                ResourceGroupName = ResourceGroup.ResourceGroupName,
                CsmTemplate = template
            };
            return Deployment.Deploy(block: block);
        }

        public void Complete()
        {
            this._tokenSource.Cancel();
        }
    }
}