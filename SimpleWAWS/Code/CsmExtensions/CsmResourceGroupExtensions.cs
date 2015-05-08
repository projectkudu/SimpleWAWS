using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<ResourceGroup> Load(this ResourceGroup resourceGroup, CsmWrapper<CsmResourceGroup> csmResourceGroup = null)
        {
            Validate.ValidateCsmResourceGroup(resourceGroup);

            if (csmResourceGroup == null)
            {
                var csmResourceGroupResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.ResourceGroup.Bind(resourceGroup));
                csmResourceGroupResponse.EnsureSuccessStatusCode();
                csmResourceGroup = await csmResourceGroupResponse.Content.ReadAsAsync<CsmWrapper<CsmResourceGroup>>();
            }

            //Not sure what to do at this point TODO
            Validate.NotNull(csmResourceGroup.tags, "csmResorucegroup.tags");

            resourceGroup.Tags = csmResourceGroup.tags;

            await Task.WhenAll(LoadSites(resourceGroup), LoadApiApps(resourceGroup), LoadGateways(resourceGroup), LoadServerFarms(resourceGroup));

            return resourceGroup;
        }
        public static async Task<ResourceGroup> LoadSites(this ResourceGroup resourceGroup)
        {
            var csmSitesResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.Sites.Bind(resourceGroup));
            csmSitesResponse.EnsureSuccessStatusCode();

            var csmSites = await csmSitesResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmSite>>();
            resourceGroup.Sites = await Task.WhenAll(csmSites.value.Select(async cs => await Load(new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, cs.name), cs)));
            return resourceGroup;
        }

        public static async Task<ResourceGroup> LoadApiApps(this ResourceGroup resourceGroup)
        {
            var csmApiAppsResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.ApiApps.Bind(resourceGroup));
            csmApiAppsResponse.EnsureSuccessStatusCode();

            var csmApiApps = await csmApiAppsResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmApiApp>>();
            resourceGroup.ApiApps = csmApiApps.value.Select(a => new ApiApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, a.name));

            return resourceGroup;
        }

        public static async Task<ResourceGroup> LoadGateways(this ResourceGroup resourceGroup)
        {
            var csmGatewaysResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.Gateways.Bind(resourceGroup));
            csmGatewaysResponse.EnsureSuccessStatusCode();

            var csmGateway = await csmGatewaysResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmGateway>>();
            resourceGroup.Gateways =csmGateway.value.Select(g => new Gateway(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, g.name));

            return resourceGroup;
        }

        public static async Task<ResourceGroup> LoadServerFarms(this ResourceGroup resourceGroup)
        {
            var csmServerFarmsResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.ServerFarms.Bind(resourceGroup));
            csmServerFarmsResponse.EnsureSuccessStatusCode();

            var csmServerFarms = await csmServerFarmsResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmServerFarm>>();
            resourceGroup.ServerFarms = csmServerFarms.value.Select(s => new ServerFarm(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, s.name));

            return resourceGroup;
        }

        public static async Task<ResourceGroup> Update(this ResourceGroup resourceGroup)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.ResourceGroup.Bind(resourceGroup), new { properties = new {}, tags = resourceGroup.Tags, location = resourceGroup.GeoRegion });
            csmResponse.EnsureSuccessStatusCode();
            return resourceGroup;
        }

        public static async Task<ResourceGroup> CreateResourceGroup(string subscriptionId, string region)
        {
            var guid = Guid.NewGuid().ToString();
            var resourceGroup = new ResourceGroup(subscriptionId, string.Join(Constants.TryResourceGroupSeparator, Constants.TryResourceGroupPrefix, region.Replace(" ", Constants.TryResourceGroupSeparator), guid))
                {
                    Tags = new Dictionary<string, string> 
                    {
                        { Constants.StartTime, DateTime.UtcNow.ToString() },
                        { Constants.IsRbacEnabled, false.ToString() },
                        { Constants.GeoRegion, region }
                    }
                };

            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.ResourceGroup.Bind(resourceGroup), new
            {
                tags = resourceGroup.Tags,
                properties = new { },
                location = region
            });

            csmResponse.EnsureSuccessStatusCode();

            return resourceGroup;
        }

        public static async Task Delete(this ResourceGroup resourceGroup, bool block)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Delete, CsmTemplates.ResourceGroup.Bind(resourceGroup));
            csmResponse.EnsureSuccessStatusCode();
            if (block)
            {
                var location = csmResponse.Headers.Location;
                if (location != null)
                {
                    var deleted = false;
                    do
                    {
                        var response = await csmClient.HttpInvoke(HttpMethod.Get, location);
                        response.EnsureSuccessStatusCode();

                        if (response.StatusCode == HttpStatusCode.OK ||
                            response.StatusCode == HttpStatusCode.NoContent)
                        {
                            deleted = true;
                        }
                        else
                        {
                            await Task.Delay(500);
                        }

                    } while(!deleted);
                }
                else
                {
                    //No idea what this means from CSM
                }

            }
        }

        public static async Task<ResourceGroup> PutInDesiredState(this ResourceGroup resourceGroup)
        {
            // If the resourceGroup is assigned, don't mess with it.
            if (!string.IsNullOrEmpty(resourceGroup.UserId)) return resourceGroup;

            //TODO: move to config
            var neededSites = 1 - resourceGroup.Sites.Count();
            var createdSites = Enumerable.Empty<Site>();

            if (neededSites > 0)
            {
                createdSites = await Task.WhenAll(Enumerable.Range(0, neededSites).Select(async n =>
                {
                    var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, SiteNameGenerator.GenerateName());
                    var csmSiteResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.Site.Bind(site), new { properties = new { }, location = resourceGroup.GeoRegion });
                    csmSiteResponse.EnsureSuccessStatusCode();

                    var csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();

                    return await Load(site, csmSite);
                }));
            }

            resourceGroup.Sites = resourceGroup.Sites.Union(createdSites);
            return resourceGroup;
        }

        public static async Task<ResourceGroup> DeleteAndCreateReplacement(this ResourceGroup resourceGroup)
        {
            var region = resourceGroup.GeoRegion;
            var subscriptionId = resourceGroup.SubscriptionId;

            var rbacEnabled = resourceGroup.IsRbacEnabled;
            var userPrincipalId = string.Empty;
            if (rbacEnabled && !string.IsNullOrEmpty(resourceGroup.UserId))
            {
                userPrincipalId = string.Concat(resourceGroup.UserId.Split('#').Last(), "#EXT#", ConfigurationManager.AppSettings["tryWebsitesTenantName"]);
            }

            if (rbacEnabled && !string.IsNullOrEmpty(userPrincipalId))
            {
                SimpleTrace.TraceInformation("{0}; {1}", AnalyticsEvents.RemoveUserFromTenant, userPrincipalId);
                var graphUser = new RbacUser
                {
                    TenantId = ConfigurationManager.AppSettings["tryWebsitesTenantId"],
                    UserId = string.Concat(userPrincipalId, "#EXT#", ConfigurationManager.AppSettings["tryWebsitesTenantName"])
                };
                var response = await graphClient.HttpInvoke(HttpMethod.Delete, CsmTemplates.GraphUser.Bind(graphUser));
                SimpleTrace.TraceInformation("{0}; {1}; {2}", AnalyticsEvents.RemoveUserFromTenantResult,
                    response.StatusCode.ToString(), response.IsSuccessStatusCode ? "success" : await response.Content.ReadAsStringAsync());
            }

            await Delete(resourceGroup, block: true);

            //if (rbacEnabled && !string.IsNullOrEmpty(userPrincipalId))
            //{
            //    SimpleTrace.TraceInformation("{0}; {1}", AnalyticsEvents.RemoveUserFromTenant, userPrincipalId);
            //    var graphUser = new RbacUser
            //    {
            //        TenantId = ConfigurationManager.AppSettings["tryWebsitesTenantId"],
            //        UserId = string.Concat(userPrincipalId, "#EXT#", ConfigurationManager.AppSettings["tryWebsitesTenantName"])
            //    };
            //    var response = await graphClient.HttpInvoke(HttpMethod.Delete, CsmTemplates.GraphUser.Bind(graphUser));
            //    SimpleTrace.TraceInformation("{0}; {1}; {2}", AnalyticsEvents.RemoveUserFromTenantResult,
            //        response.StatusCode.ToString(), response.IsSuccessStatusCode ? "success" : await response.Content.ReadAsStringAsync());
            //}
            return await PutInDesiredState(await CreateResourceGroup(subscriptionId, region));
        }

        public static async Task<ResourceGroup> MarkInUse(this ResourceGroup resourceGroup, string userId, TimeSpan lifeTime, AppService appService)
        {
            resourceGroup.Tags[Constants.UserId] = userId;
            resourceGroup.Tags[Constants.StartTime] = DateTime.UtcNow.ToString();
            resourceGroup.Tags[Constants.LifeTimeInMinutes] = lifeTime.TotalMinutes.ToString();
            resourceGroup.Tags[Constants.AppService] = appService.ToString();
            return await Update(resourceGroup);
        }

        public static async Task<bool> AddResourceGroupRbac(this ResourceGroup resourceGroup, string puidOrAltSec, string emailAddress)
        {
            return (await Task.WhenAll(
                resourceGroup.Sites.Select(s => s.AddRbacAccess(puidOrAltSec, emailAddress))
                .Concat(resourceGroup.ApiApps.Select(s => s.AddRbacAccess(puidOrAltSec, emailAddress)))
                .Concat(resourceGroup.Gateways.Select(s => s.AddRbacAccess(puidOrAltSec,emailAddress)))
                .Concat(resourceGroup.ServerFarms.Select(s => s.AddRbacAccess(puidOrAltSec, emailAddress)))))
                .All(e => e);
        }

        private static bool IsSimpleWaws(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return !string.IsNullOrEmpty(csmResourceGroup.name) && csmResourceGroup.name.StartsWith(Constants.TryResourceGroupPrefix) && csmResourceGroup.properties.provisioningState == "Succeeded";
        }
    }
}