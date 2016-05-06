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
            await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.WebsitesRegister.Bind(subscription));
            await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.AppServiceRegister.Bind(subscription));
            await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.StorageRegister.Bind(subscription));

            var csmResourceGroupsRespnose = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroups.Bind(subscription));
            await csmResourceGroupsRespnose.EnsureSuccessStatusCodeWithFullError();

            var csmResourceGroups = await csmResourceGroupsRespnose.Content.ReadAsAsync<CsmArrayWrapper<CsmResourceGroup>>();

            var deleteBadResourceGroupsTasks = csmResourceGroups.value
                .Where(r => r.tags != null && r.tags.ContainsKey("Bad") && r.properties.provisioningState != "Deleting")
                .Select(async r => await Delete(await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r, loadSubResources: false), block: false));

            var csmSubscriptionResourcesReponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.SubscriptionResources.Bind(subscription));
            await csmSubscriptionResourcesReponse.EnsureSuccessStatusCodeWithFullError();
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

            result.ToDelete = subscription.ResourceGroups
                .GroupBy(s => s.GeoRegion)
                .Select(g => new { Region = g.Key, ResourceGroups = g.Select(r => r), Count = g.Count() })
                .Where(g => g.Count > 1)
                .Select(g => g.ResourceGroups.Where(rg => string.IsNullOrEmpty(rg.UserId)).Skip(1))
                .SelectMany(i => i);

            result.Ready = subscription.ResourceGroups.Where(rg => !result.ToDelete.Any(drg => drg.ResourceGroupName == rg.ResourceGroupName));

            return result;
        }

    }
}