namespace SimpleWAWS.Models.CsmModels
{
    public class CsmDeployment
    {
        public string DeploymentName { get; set; }

        public string ResourceGroupName { get; set; }

        public string SubscriptionId { get; set; }

        public object CsmTemplate { get; set; }

        public string Status { get; set; }
    }
}