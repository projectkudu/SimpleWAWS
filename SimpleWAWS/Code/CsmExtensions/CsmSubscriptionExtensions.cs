using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<Subscription> Load(this Subscription subscription)
        {
            Validate.ValidateCsmSubscription(subscription);

            var csmResourceGroupsRespnose = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.ResourceGroups.Bind(subscription));
            csmResourceGroupsRespnose.EnsureSuccessStatusCode();

            var csmResourceGroups = await csmResourceGroupsRespnose.Content.ReadAsAsync<CsmArrayWrapper<CsmResourceGroup>>();

            var deleteBadResourceGroupsTasks = csmResourceGroups.value
                .Where(r => r.tags.ContainsKey("Bad") && r.properties.provisioningState != "Deleting")
                .Select(async r => await Delete(await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r, loadSubResources: false), block: false));

            subscription.ResourceGroups = await csmResourceGroups.value
                .Where(r => IsSimpleWaws(r))
                .Select(async r => await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r))
                .IgnoreAndFilterFailures();

            await deleteBadResourceGroupsTasks.IgnoreFailures().WhenAll();
            return subscription;
        }

        public static async Task<Subscription> MakeTrialSubscription(this Subscription subscription)
        {
            //Make sure to register for AppServices RP at least once for each sub
            await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.AppServiceRegister.Bind(subscription));

            var geoRegions = SimpleSettings.GeoRegions.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());

            var newResourceGroups = await geoRegions.Where(g => !subscription.ResourceGroups.Any(rg => rg.ResourceGroupName.StartsWith(string.Format("{0}_{1}", Constants.TryResourceGroupPrefix, g.Replace(" ", Constants.TryResourceGroupSeparator)))))
                .Select(g => CreateResourceGroup(subscription.SubscriptionId, g))
                .WhenAll();

            subscription.ResourceGroups = subscription.ResourceGroups.Union(newResourceGroups);

            if (subscription.ResourceGroups.Count() > geoRegions.Count())
            {
                //we have extra resourceGroups. We should delete it.
            }

            // Ignore all failures with putting a resourceGroup in the desired state for us
            // But only load the ones marked as ready.
            subscription.ResourceGroups = await subscription.ResourceGroups.Select(rg => PutInDesiredState(rg)).IgnoreAndFilterFailures();
            return subscription;
        }
    }
}