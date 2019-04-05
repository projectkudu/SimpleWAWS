using Newtonsoft.Json;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<Subscription> SubscriptionCleanup(this Subscription subscription)
        {
            return await Load(subscription);
        }
        public static async Task<Subscription> Load(this Subscription subscription)
        {
            Validate.ValidateCsmSubscription(subscription);
            //Make sure to register for AppServices RP at least once for each 
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.WebsitesRegister.Bind(subscription));
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.StorageRegister.Bind(subscription));

            var csmResourceGroups = await subscription.LoadResourceGroupsForSubscription();
            var csmSubscriptionResourcesReponse = await GetClient(subscription.Type).HttpInvoke(HttpMethod.Get, ArmUriTemplates.SubscriptionResources.Bind(subscription));

            await csmSubscriptionResourcesReponse.EnsureSuccessStatusCodeWithFullError();

            var csmSubscriptionResources =
                await csmSubscriptionResourcesReponse.Content.ReadAsAsync<CsmArrayWrapper<object>>();

            var goodResourceGroups = csmResourceGroups.value
                .Where(r => IsValidResource(r, subscription.Type))
                .Select(r => new
                {
                    ResourceGroup = r,
                    Resources = csmSubscriptionResources.value.Where(
                                resource => resource.id.IndexOf(r.id, StringComparison.OrdinalIgnoreCase) != -1)
                });

            subscription.ResourceGroups = await goodResourceGroups
            .Select(async r => await Load(new ResourceGroup(subscription.SubscriptionId, r.ResourceGroup.name), r.ResourceGroup, r.Resources))
            .IgnoreAndFilterFailures();
            return subscription;
        }

        public static async Task<CsmArrayWrapper<CsmResourceGroup>> LoadResourceGroupsForSubscription(this Subscription subscription)
        {
            var csmResourceGroupsResponse = await GetClient(subscription.Type).HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroups.Bind(subscription));
            await csmResourceGroupsResponse.EnsureSuccessStatusCodeWithFullError();

            return await csmResourceGroupsResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmResourceGroup>>();
        }

        public static SubscriptionStats GetSubscriptionStats(this Subscription subscription)
        {
            var result = new SubscriptionStats();
            {
                var vscodeTemplates = TemplatesManager.GetTemplates();

                result.ToDelete = subscription.ResourceGroups
                    .Where(b=> string.IsNullOrEmpty(b.DeployedTemplateName) || string.IsNullOrEmpty(b.SiteGuid));
            }

            //TODO:Also delete RGs that are not in subscription.GeoRegions
            result.Ready = subscription.ResourceGroups.Where(rg => !result.ToDelete.Any(drg => drg.ResourceGroupName == rg.ResourceGroupName));
            SimpleTrace.TraceInformation($"MakeTrialSubscription for: {subscription.Type.ToString()} : {subscription.SubscriptionId} -> Ready:{result.Ready.Count()} -> ToDelete:{result.ToDelete.Count()}");
            return result;
        }

    }
}