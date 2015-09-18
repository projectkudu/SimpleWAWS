using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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

            //Make sure to register for AppServices RP at least once for each sub
            await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.WebsitesRegister.Bind(subscription));
            await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.AppServiceRegister.Bind(subscription));

            var csmResourceGroupsRespnose = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.ResourceGroups.Bind(subscription));
            csmResourceGroupsRespnose.EnsureSuccessStatusCode();

            var csmResourceGroups = await csmResourceGroupsRespnose.Content.ReadAsAsync<CsmArrayWrapper<CsmResourceGroup>>();

            var deleteBadResourceGroupsTasks = csmResourceGroups.value
                .Where(r => r.tags != null && r.tags.ContainsKey("Bad") && r.properties.provisioningState != "Deleting")
                .Select(async r => await Delete(await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r, loadSubResources: false), block: false));

            var csmSubscriptionResourcesReponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.SubscriptionResources.Bind(subscription));
            csmSubscriptionResourcesReponse.EnsureSuccessStatusCode();
            var csmSubscriptionResources = await csmSubscriptionResourcesReponse.Content.ReadAsAsync<CsmArrayWrapper<object>>();

            var goodResourceGroups = csmResourceGroups.value
                .Where(r => IsSimpleWaws(r))
                .Select(r => new
                {
                    ResourceGroup = r,
                    Resources = csmSubscriptionResources.value.Where(resource => resource.id.IndexOf(r.id, StringComparison.OrdinalIgnoreCase) != -1)
                });



            subscription.ResourceGroups = await goodResourceGroups
                .Select(async r => await Load(new ResourceGroup(subscription.SubscriptionId, r.ResourceGroup.name), r.ResourceGroup, r.Resources))
                .IgnoreAndFilterFailures();

            await deleteBadResourceGroupsTasks.IgnoreFailures().WhenAll();
            return subscription;
        }

        public static MakeSubscriptionFreeTrialResult MakeTrialSubscription(this Subscription subscription)
        {
            var result = new MakeSubscriptionFreeTrialResult();
            var geoRegions = SimpleSettings.GeoRegions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());

            result.ToCreateInRegions = geoRegions
                .Where(g =>
                       !subscription.ResourceGroups
                           .Any(rg => rg.ResourceGroupName.StartsWith(string.Format(CultureInfo.InvariantCulture, "{0}_{1}", Constants.TryResourceGroupPrefix, g.Replace(" ", Constants.TryResourceGroupSeparator)), StringComparison.OrdinalIgnoreCase)));

            result.Ready = subscription.ResourceGroups;

            if (subscription.ResourceGroups.Count() > geoRegions.Count())
            {
                //we have extra resourceGroups. We should delete it.
            }

            result.ToDelete = Enumerable.Empty<ResourceGroup>();

            return result;
        }

    }
}