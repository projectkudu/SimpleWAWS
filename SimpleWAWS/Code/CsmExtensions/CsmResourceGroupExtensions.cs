using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<ResourceGroup> Load(this ResourceGroup resourceGroup, CsmWrapper<CsmResourceGroup> csmResourceGroup = null, bool loadSubResources = true)
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
            if (loadSubResources)
            {
                await Task.WhenAll(LoadSites(resourceGroup),
                                   LoadApiApps(resourceGroup),
                                   LoadGateways(resourceGroup),
                                   LoadLogicApps(resourceGroup),
                                   LoadServerFarms(resourceGroup));
            }

            return resourceGroup;
        }

        public static async Task<ResourceGroup> LoadSites(this ResourceGroup resourceGroup)
        {
            var csmSitesResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.Sites.Bind(resourceGroup));
            csmSitesResponse.EnsureSuccessStatusCode();

            var csmSites = await csmSitesResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmSite>>();
            resourceGroup.Sites = await csmSites.value.Select(async cs => await Load(new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, cs.name), cs)).WhenAll();
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

        public static async Task<ResourceGroup> LoadLogicApps(this ResourceGroup resourceGroup)
        {
            var csmLogicAppsResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.LogicApps.Bind(resourceGroup));
            csmLogicAppsResponse.EnsureSuccessStatusCode();

            var csmLogicApps = await csmLogicAppsResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmLogicApp>>();
            resourceGroup.LogicApps = csmLogicApps.value.Select(a => new LogicApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, a.name));

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
            // Mark as a "Bad" resourceGroup just in case the delete fails for any reason.
            // Also since this is a potentially bad resourceGroup, ignore failure
            resourceGroup.Tags["Bad"] = "1";
            await Update(resourceGroup).IgnoreFailure();

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

                        //TODO: How does this handle failing to delete a resourceGroup?
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
            // If the resourceGroup is assigned, don't mess with it
            if (!string.IsNullOrEmpty(resourceGroup.UserId)) return resourceGroup;

            //TODO: move to config
            var neededSites = 1 - resourceGroup.Sites.Where(s => s.IsSimpleWAWSOriginalSite).Count();
            var createdSites = Enumerable.Empty<Site>();

            if (neededSites > 0)
            {
                createdSites = await Enumerable.Range(0, neededSites).Select(async n =>
                {
                    var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, SiteNameGenerator.GenerateName());
                    var csmSiteResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.Site.Bind(site), new { properties = new { }, location = resourceGroup.GeoRegion });
                    csmSiteResponse.EnsureSuccessStatusCode();

                    var csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();

                    return await Load(site, csmSite);
                }).WhenAll();
            }

            resourceGroup.Sites = resourceGroup.Sites.Union(createdSites);

            //var csmTemplateString = string.Empty;

            ////using (var reader = new StreamReader(HostingEnvironment.MapPath("~/App_Data/commonApiApps.json")))
            //using (var reader = new StreamReader(@"D:\scratch\repos\SimpleWAWS\SimpleWAWS\App_Data\commonApiApps.json"))
            //{
            //    csmTemplateString = await reader.ReadToEndAsync();
            //}

            //var gatewayName = resourceGroup.Gateways.Count() != 0
            //    ? resourceGroup.Gateways.Select(s => s.GatewayName).First()
            //    : Guid.NewGuid().ToString().Replace("-", "");
            //csmTemplateString = csmTemplateString.Replace("{{gatewayName}}", gatewayName);

            //var deployment = new CsmDeployment
            //{
            //    DeploymentName = resourceGroup.ResourceUniqueId,
            //    SubscriptionId = resourceGroup.SubscriptionId,
            //    ResourceGroupName = resourceGroup.ResourceGroupName,
            //    CsmTemplate = JsonConvert.DeserializeObject<JToken>(csmTemplateString),
            //};

            //await deployment.Deploy(block: true);

            //await resourceGroup.Load();

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

        public static async Task<bool> AddResourceGroupRbac(this ResourceGroup resourceGroup, string puidOrAltSec, string emailAddress)
        {
            var objectId = await GetUserObjectId(puidOrAltSec, emailAddress);

            if (string.IsNullOrEmpty(objectId)) return false;

            return (await
                new[] { resourceGroup.AddRbacAccess(objectId) }
                .Concat(resourceGroup.Sites.Select(s => s.AddRbacAccess(objectId)))
                .Concat(resourceGroup.ApiApps.Select(s => s.AddRbacAccess(objectId)))
                .Concat(resourceGroup.Gateways.Select(s => s.AddRbacAccess(objectId)))
                .Concat(resourceGroup.LogicApps.Select(s => s.AddRbacAccess(objectId)))
                .Concat(resourceGroup.ServerFarms.Select(s => s.AddRbacAccess(objectId)))
                .WhenAll())
                .All(e => e);
        }

        private static bool IsSimpleWaws(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return !string.IsNullOrEmpty(csmResourceGroup.name) &&
                csmResourceGroup.name.StartsWith(Constants.TryResourceGroupPrefix) &&
                csmResourceGroup.properties.provisioningState == "Succeeded" &&
                !csmResourceGroup.tags.ContainsKey("Bad");
        }
    }
}