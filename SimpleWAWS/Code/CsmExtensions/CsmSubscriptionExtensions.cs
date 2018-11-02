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
            return await Load(subscription, deleteBadResourceGroups: true);
        }
        public static async Task<Subscription> Load(this Subscription subscription, bool deleteBadResourceGroups = true)
        {
            Validate.ValidateCsmSubscription(subscription);
            //Make sure to register for AppServices RP at least once for each 
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.WebsitesRegister.Bind(subscription));
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.AppServiceRegister.Bind(subscription));
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.StorageRegister.Bind(subscription));

            var csmResourceGroups = await subscription.LoadResourceGroupsForSubscription();
            if (deleteBadResourceGroups)
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();

                var deleteBadResourceGroupsTasks = csmResourceGroups.value
                    //Some orphaned resourcegroups can have no tags. Okay to clean once in a while since they dont have any sites either
                    .Where(r => ((r.tags == null && IsSimpleWawsResourceName(r))
                                || (r.tags != null && ((r.tags.ContainsKey("Bad") ||
                                    (subscription.Type == SubscriptionType.AppService ?
                                    !r.tags.ContainsKey(Constants.FunctionsContainerDeployed)
                                    : subscription.Type == SubscriptionType.Linux ? !r.tags.ContainsKey(Constants.LinuxAppDeployed) : !r.tags.ContainsKey(Constants.TemplateName)
                                    ))
                                  ))
                                && r.properties.provisioningState != "Deleting"))
                    .Where(p => !resourceManager.GetAllLoadedResourceGroups().Any(p2 => string.Equals(p.id, p2.CsmId, StringComparison.OrdinalIgnoreCase)))
                    .Select(async r => await Delete(await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r, loadSubResources: false), block: false));

                await deleteBadResourceGroupsTasks.IgnoreFailures().WhenAll();

                csmResourceGroups = await subscription.LoadResourceGroupsForSubscription();
            }
            var csmSubscriptionResourcesReponse = await GetClient(subscription.Type).HttpInvoke(HttpMethod.Get, ArmUriTemplates.SubscriptionResources.Bind(subscription));

            await csmSubscriptionResourcesReponse.EnsureSuccessStatusCodeWithFullError();

            var csmSubscriptionResources =
                await csmSubscriptionResourcesReponse.Content.ReadAsAsync<CsmArrayWrapper<object>>();

            var goodResourceGroups = csmResourceGroups.value
                .Where(r => subscription.Type == SubscriptionType.AppService ? IsSimpleWaws(r)
                          : subscription.Type == SubscriptionType.Linux ? IsLinuxResource(r)
                          : IsVSCodeLinuxResource(r))
                .Select(r => new
                {
                    ResourceGroup = r,
                    Resources = csmSubscriptionResources.value.Where(
                                resource => resource.id.IndexOf(r.id, StringComparison.OrdinalIgnoreCase) != -1)
                });

            subscription.ResourceGroups = await goodResourceGroups
            .Select(async r => await Load(new ResourceGroup(subscription.SubscriptionId, r.ResourceGroup.name), r.ResourceGroup, r.Resources))
            .IgnoreAndFilterFailures();

            if (deleteBadResourceGroups)
            {
                var resourceManager = await ResourcesManager.GetInstanceAsync();
                var deleteDuplicateResourceGroupsTasks = csmResourceGroups.value
                    .Where(p => subscription.ResourceGroups.All(p2 => p2.CsmId != p.id) && !IsSimpleWawsResourceActive(p))
                    .Where(p => !resourceManager.GetAllLoadedResourceGroups().Any(p2 => string.Equals(p.id, p2.CsmId, StringComparison.OrdinalIgnoreCase)))
                    .Select(async r => await Delete(await Load(new ResourceGroup(subscription.SubscriptionId, r.name), r, loadSubResources: false), block: false));
                await deleteDuplicateResourceGroupsTasks.IgnoreFailures().WhenAll();

            }
            return subscription;
        }
        public static async Task<Subscription> LoadMonitoringToolSubscription(this Subscription subscription)
        {
            Validate.ValidateCsmSubscription(subscription);
            //Make sure to register for AppServices RP at least once for each 
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.WebsitesRegister.Bind(subscription));
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.AppServiceRegister.Bind(subscription));
            await GetClient(subscription.Type).HttpInvoke(HttpMethod.Post, ArmUriTemplates.StorageRegister.Bind(subscription));

            var csmResourceGroups = await subscription.LoadResourceGroupsForSubscription();
            var csmSubscriptionResourcesReponse = await GetClient(subscription.Type).HttpInvoke(HttpMethod.Get, ArmUriTemplates.SubscriptionResources.Bind(subscription));

            await csmSubscriptionResourcesReponse.EnsureSuccessStatusCodeWithFullError();

            var csmSubscriptionResources =
                await csmSubscriptionResourcesReponse.Content.ReadAsAsync<CsmArrayWrapper<object>>();

            var goodResourceGroups = csmResourceGroups.value
                .Where(r => r.name == SimpleSettings.MonitoringToolsResourceGroupName)
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

        public static MakeSubscriptionFreeTrialResult MakeTrialSubscription(this Subscription subscription)
        {
            var result = new MakeSubscriptionFreeTrialResult();
            if (subscription.Type != SubscriptionType.VSCodeLinux)
            {
                result.ToCreateTemplates = new List<TemplateStats>();
                result.ToCreateInRegions = subscription.GeoRegions
                .Where(g => !subscription.ResourceGroups
                           .Any(rg => rg.ResourceGroupName.StartsWith(string.Format(CultureInfo.InvariantCulture, "{0}_{1}", Constants.TryResourceGroupPrefix, g.Replace(" ", Constants.TryResourceGroupSeparator)), StringComparison.OrdinalIgnoreCase)))
                           .Concat(subscription.ResourceGroups
                                    .GroupBy(s => s.GeoRegion)
                                    .Select(g => new { Region = g.Key, ResourceGroups = g.Select(r => r), RemainingCount = (subscription.ResourceGroupsPerGeoRegion) - g.Count() })
                                    .Where(g => g.RemainingCount > 0)
                                    .Select(g => Enumerable.Repeat(g.Region, g.RemainingCount))
                                    .Select(i => i)
                                    .SelectMany(i => i)
                                    );
                result.ToDelete = subscription.ResourceGroups
                .GroupBy(s => s.GeoRegion)
                .Select(g => new { Region = g.Key, ResourceGroups = g.Select(r => r), Count = g.Count() })
                .Where(g => g.Count > subscription.ResourceGroupsPerGeoRegion)
                .Select(g => g.ResourceGroups.Where(rg => string.IsNullOrEmpty(rg.UserId)).Skip((subscription.ResourceGroupsPerGeoRegion)))
                .SelectMany(i => i);

            }
            else if (subscription.Type == SubscriptionType.VSCodeLinux)
            {
                result.ToCreateInRegions = new List<string>();
                var vscodeTemplates = TemplatesManager.GetTemplates().Where(a => a.AppService == AppService.VSCodeLinux);
                var temp = subscription.ResourceGroups
                    .Where(b=> !string.IsNullOrEmpty(b.DeployedTemplateName) && !string.IsNullOrEmpty(b.SiteGuid))
                    .GroupBy(a => a.DeployedTemplateName)
                    .Select(g => new
                    TemplateStats
                    {
                        TemplateName = g.Key,
                        RemainingCount = subscription.ResourceGroupsPerTemplate - g.Count()
                    });

                var temp2 = new List<TemplateStats>();
                foreach (var remaining in vscodeTemplates)
                {
                    if (!temp.Any(a => a.TemplateName == remaining.Name))
                    {
                        temp = temp.Union(new List<TemplateStats> { new TemplateStats{ TemplateName = remaining.Name, RemainingCount = subscription.ResourceGroupsPerTemplate } });
                    }
                    var count = temp.First(a => a. TemplateName == remaining.Name).RemainingCount;
                    temp2.Add(new TemplateStats { TemplateName = remaining.Name, RemainingCount = count });
                }
                result.ToCreateTemplates = temp2;
                result.ToDelete = subscription.ResourceGroups
                        .GroupBy(s => s.DeployedTemplateName)
                        .Select(g => new { TemplateName = g.Key, ResourceGroups = g.Select(r => r), ExtraCount = g.Count() - (subscription.ResourceGroupsPerTemplate) })
                        .Where(g => g.ExtraCount > 0)
                        .Select(g => g.ResourceGroups.Where(rg => string.IsNullOrEmpty(rg.UserId)).Skip((subscription.ResourceGroupsPerTemplate)))
                        .SelectMany(i => i).Take(2);
            }

            //TODO:Also delete RGs that are not in subscription.GeoRegions
            result.Ready = subscription.ResourceGroups.Where(rg => !result.ToDelete.Any(drg => drg.ResourceGroupName == rg.ResourceGroupName));
            SimpleTrace.TraceInformation($"MakeTrialSubscription for: {subscription.Type.ToString()} : {subscription.SubscriptionId} -> Ready:{result.Ready.Count()} -> ToCreate:{result.ToCreateInRegions.Count()}  -> ToDelete:{result.ToDelete.Count()}");
            return result;
        }
        public static MakeSubscriptionFreeTrialResult GetMonitoringToolsResource(this Subscription subscription)
        {
            var result = new MakeSubscriptionFreeTrialResult();

            result.Ready = subscription.ResourceGroups.Where(rg => rg.ResourceGroupName == SimpleSettings.MonitoringToolsResourceGroupName);

            return result;
        }
    }
}