using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
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
        
        public static async Task<Subscription> SubscriptionCleanup(this Subscription subscription) {
            return await Load(subscription, deleteBadResourceGroups: true, cleanupSubscriptionOnly: true);

        }
        public static async Task<Subscription> Load(this Subscription subscription, bool deleteBadResourceGroups = true, bool cleanupSubscriptionOnly = false)
        {
            Validate.ValidateCsmSubscription(subscription);
            //Make sure to register for AppServices RP at least once for each sub
            if (!cleanupSubscriptionOnly)
            {
                await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.WebsitesRegister.Bind(subscription));
                await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.AppServiceRegister.Bind(subscription));
                await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.StorageRegister.Bind(subscription));
            }

                var csmResourceGroups = await subscription.LoadResourceGroupsForSubscription();
                if (deleteBadResourceGroups && !cleanupSubscriptionOnly)
                {
                    var deleteBadResourceGroupsTasks = csmResourceGroups.value
                        //Some orphaned resourcegroups can have no tags. Okay to clean once in a while since they dont have any sites either
                        .Where(r =>   ((r.tags == null && IsSimpleWawsResourceName(r)) 
                                    || (r.tags != null && ((r.tags.ContainsKey("Bad") ||
                                       (subscription.Type == SubscriptionType.AppService ? !r.tags.ContainsKey("FunctionsContainerDeployed") : !r.tags.ContainsKey(Constants.SubscriptionType))))) 
                                    &&  r.properties.provisioningState != "Deleting"))
                        .Select(async r => await Delete(await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r, loadSubResources: false), block: false));
              
                    await deleteBadResourceGroupsTasks.IgnoreFailures().WhenAll();

                    csmResourceGroups = await subscription.LoadResourceGroupsForSubscription();
                }
                var csmSubscriptionResourcesReponse = await GetClient(subscription.Type).HttpInvoke(HttpMethod.Get, ArmUriTemplates.SubscriptionResources.Bind(subscription));

                await csmSubscriptionResourcesReponse.EnsureSuccessStatusCodeWithFullError();

                var csmSubscriptionResources =
                    await csmSubscriptionResourcesReponse.Content.ReadAsAsync<CsmArrayWrapper<object>>();

                var goodResourceGroups = csmResourceGroups.value
                    .Where(r => subscription.Type == SubscriptionType.AppService?IsSimpleWaws(r) : IsJenkinsResource(r))
                    .Select(r => new
                    {
                        ResourceGroup = r,
                        Resources = csmSubscriptionResources.value.Where(
                                    resource => resource.id.IndexOf(r.id, StringComparison.OrdinalIgnoreCase) != -1)
                    });

                    subscription.ResourceGroups = await goodResourceGroups
                    .Select( async r => await Load(new ResourceGroup(subscription.SubscriptionId, r.ResourceGroup.name),r.ResourceGroup, r.Resources))
                    .IgnoreAndFilterFailures();

            if (deleteBadResourceGroups)
            {
                var deleteDuplicateResourceGroupsTasks = goodResourceGroups.Where(p => !subscription.ResourceGroups.Any(p2 => p2.CsmId == p.ResourceGroup.id))
                    .Select(async r => await Delete(await Load(new ResourceGroup(subscription.SubscriptionId, r.ResourceGroup.name), r.ResourceGroup, loadSubResources: false), block: false));
                await deleteDuplicateResourceGroupsTasks.IgnoreFailures().WhenAll();
            }
            return subscription;

        }

        private static  async Task<CsmArrayWrapper<CsmResourceGroup>> LoadResourceGroupsForSubscription(this Subscription subscription)
        {
            var csmResourceGroupsRespnose = await GetClient(subscription.Type).HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroups.Bind(subscription));
            await csmResourceGroupsRespnose.EnsureSuccessStatusCodeWithFullError();

            return  await csmResourceGroupsRespnose.Content.ReadAsAsync<CsmArrayWrapper<CsmResourceGroup>>();
        }

        public static MakeSubscriptionFreeTrialResult MakeTrialSubscription(this Subscription subscription)
        {
            var result = new MakeSubscriptionFreeTrialResult();
            var geoRegions = subscription.Type==SubscriptionType.AppService?
                            SimpleSettings.GeoRegions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim())
                            :SimpleSettings.JenkinsGeoRegions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());

            result.ToCreateInRegions = geoRegions
                .Where(g => !subscription.ResourceGroups
                           .Any(rg => rg.ResourceGroupName.StartsWith(string.Format(CultureInfo.InvariantCulture, "{0}_{1}", Constants.TryResourceGroupPrefix, g.Replace(" ", Constants.TryResourceGroupSeparator)), StringComparison.OrdinalIgnoreCase)));

            result.ToDelete = subscription.ResourceGroups
                .GroupBy(s => s.GeoRegion)
                .Select(g => new { Region = g.Key, ResourceGroups = g.Select(r => r), Count = g.Count() })
                .Where(g => g.Count > (subscription.Type==SubscriptionType.AppService? 1: SimpleSettings.JenkinsResourceGroupsPerRegion))
                .Select(g => g.ResourceGroups.Where(rg => string.IsNullOrEmpty(rg.UserId)).Skip((subscription.Type == SubscriptionType.AppService ? 1 : SimpleSettings.JenkinsResourceGroupsPerRegion)))
                .SelectMany(i => i);

            result.Ready = subscription.ResourceGroups.Where(rg => !result.ToDelete.Any(drg => drg.ResourceGroupName == rg.ResourceGroupName));

            return result;
        }
    }
}