using SimpleWAWS.Models;
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
            subscription.ResourceGroups = await Task.WhenAll(csmResourceGroups.value.Where(r => IsSimpleWaws(r)).Select(async r => await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r)));

            return subscription;
        }

        public static async Task<Subscription> MakeTrialSubscription(this Subscription subscription)
        {
            //Make sure to register for AppServices RP at least once for each sub
            await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.AppServiceRegister.Bind(subscription));

            var geoRegions = ConfigurationManager.AppSettings["geoRegions"].Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim());

            var newResourceGroups = await Task.WhenAll(geoRegions.Where(g => !subscription.ResourceGroups.Any(rg => rg.ResourceGroupName.StartsWith(string.Format("{0}_{1}", Constants.TryResourceGroupPrefix, g.Replace(" ", Constants.TryResourceGroupSeparator)))))
                                                                 .Select(g => CreateResourceGroup(subscription.SubscriptionId, g)));

            subscription.ResourceGroups = subscription.ResourceGroups.Union(newResourceGroups);

            await Task.WhenAll(subscription.ResourceGroups.Select(rg => PutInDesiredState(rg)));

            return subscription;
        }
    }
}