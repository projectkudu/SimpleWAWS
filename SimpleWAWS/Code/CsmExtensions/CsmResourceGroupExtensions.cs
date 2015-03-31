using SimpleWAWS.Models;
using System;
using System.Collections.Generic;
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

            var csmSitesResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.Sites.Bind(resourceGroup));
            csmSitesResponse.EnsureSuccessStatusCode();

            var csmSites = await csmSitesResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmSite>>();
            resourceGroup.Sites = await Task.WhenAll(csmSites.value.Select(async cs => await Load(new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, cs.name), cs)));

            return resourceGroup;
        }

        public static async Task<ResourceGroup> Update(this ResourceGroup resourceGroup)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.ResourceGroup.Bind(resourceGroup), new { properties = new {}, tags = resourceGroup.Tags });
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
            await Delete(resourceGroup, block: true);
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

        private static bool IsSimpleWaws(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return !string.IsNullOrEmpty(csmResourceGroup.name) && csmResourceGroup.name.StartsWith(Constants.TryResourceGroupPrefix) && csmResourceGroup.properties.provisioningState == "Succeeded";
        }
    }
}