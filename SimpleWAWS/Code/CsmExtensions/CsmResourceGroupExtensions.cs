using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
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
        public static async Task<ResourceGroup> Load(this ResourceGroup resourceGroup, CsmWrapper<CsmResourceGroup> csmResourceGroup = null, IEnumerable<CsmWrapper<object>> resources = null, bool loadSubResources = true)
        {
            Validate.ValidateCsmResourceGroup(resourceGroup);

            if (csmResourceGroup == null)
            {
                var csmResourceGroupResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroup.Bind(resourceGroup));
                await csmResourceGroupResponse.EnsureSuccessStatusCodeWithFullError();
                csmResourceGroup = await csmResourceGroupResponse.Content.ReadAsAsync<CsmWrapper<CsmResourceGroup>>();
            }

            //Not sure what to do at this point TODO
            Validate.NotNull(csmResourceGroup.tags, "csmResorucegroup.tags");

            resourceGroup.Tags = csmResourceGroup.tags;

            if (resources == null)
            {
                var csmResourceGroupResourcesResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroupResources.Bind(resourceGroup));
                await csmResourceGroupResourcesResponse.EnsureSuccessStatusCodeWithFullError();
                resources = (await csmResourceGroupResourcesResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
            }



            if (loadSubResources)
            {
                await Task.WhenAll(LoadSites(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/sites", StringComparison.OrdinalIgnoreCase))),
                                   LoadApiApps(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.AppService/apiapps", StringComparison.OrdinalIgnoreCase))),
                                   LoadGateways(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.AppService/gateways", StringComparison.OrdinalIgnoreCase))),
                                   LoadLogicApps(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Logic/workflows", StringComparison.OrdinalIgnoreCase))),
                                   LoadServerFarms(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/serverFarms", StringComparison.OrdinalIgnoreCase))),
                                   LoadStorageAccounts(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Storage/storageAccounts", StringComparison.OrdinalIgnoreCase))));
            }

            return resourceGroup;
        }

        // Full Site Load
        public static async Task<ResourceGroup> LoadSites(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> sites = null)
        {
            var csmSitesResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.Sites.Bind(resourceGroup));
            await csmSitesResponse.EnsureSuccessStatusCodeWithFullError();

            var csmSites = await csmSitesResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmSite>>();
            resourceGroup.Sites = await csmSites.value.Select(async cs => await Load(new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, cs.name), cs)).WhenAll();
            return resourceGroup;
        }

        //Shallow load
        public static async Task<ResourceGroup> LoadApiApps(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> apiApps = null)
        {
            if (apiApps == null)
            {
                var csmApiAppsResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.ApiApps.Bind(resourceGroup));
                await csmApiAppsResponse.EnsureSuccessStatusCodeWithFullError();
                apiApps = (await csmApiAppsResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
            }

            resourceGroup.ApiApps = apiApps.Select(a => new ApiApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, a.name));

            return resourceGroup;
        }

        //Shallow load
        public static async Task<ResourceGroup> LoadLogicApps(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> logicApps = null)
        {
            if (logicApps == null)
            {
                var csmLogicAppsResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.LogicApps.Bind(resourceGroup));
                await csmLogicAppsResponse.EnsureSuccessStatusCodeWithFullError();
                logicApps = (await csmLogicAppsResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
            }

            resourceGroup.LogicApps = logicApps.Select(a => new LogicApp(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, a.name));

            return resourceGroup;
        }

        //Shallow load
        public static async Task<ResourceGroup> LoadGateways(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> gateways = null)
        {
            if (gateways == null)
            {
                var csmGatewaysResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.Gateways.Bind(resourceGroup));
                await csmGatewaysResponse.EnsureSuccessStatusCodeWithFullError();
                gateways = (await csmGatewaysResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
            }

            resourceGroup.Gateways = gateways.Select(g => new Gateway(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, g.name));

            return resourceGroup;
        }

        //Shallow load
        public static async Task<ResourceGroup> LoadServerFarms(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> serverFarms = null)
        {
            if (serverFarms == null)
            {
                var csmServerFarmsResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.ServerFarms.Bind(resourceGroup));
                await csmServerFarmsResponse.EnsureSuccessStatusCodeWithFullError();
                serverFarms = (await csmServerFarmsResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
            }

            resourceGroup.ServerFarms = serverFarms.Select(s => new ServerFarm(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, s.name));

            return resourceGroup;
        }

        public static async Task<ResourceGroup> LoadStorageAccounts(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> storageAccounts = null)
        {
            if (storageAccounts == null)
            {
                var csmStorageAccountsResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.StorageAccounts.Bind(resourceGroup));
                await csmStorageAccountsResponse.EnsureSuccessStatusCodeWithFullError();
                storageAccounts = (await csmStorageAccountsResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
            }

            resourceGroup.StorageAccounts = await storageAccounts.Select(async s => await Load(new StorageAccount(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, s.name), null)).WhenAll();

            return resourceGroup;
        }

        public static async Task<ResourceGroup> Update(this ResourceGroup resourceGroup)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, ArmUriTemplates.ResourceGroup.Bind(resourceGroup), new { properties = new {}, tags = resourceGroup.Tags, location = resourceGroup.GeoRegion });
            await csmResponse.EnsureSuccessStatusCodeWithFullError();
            return resourceGroup;
        }

        public static async Task<ResourceGroup> CreateResourceGroup(string subscriptionId, string region)
        {
            var guid = Guid.NewGuid().ToString();
            var resourceGroup = new ResourceGroup(subscriptionId, string.Join(Constants.TryResourceGroupSeparator, Constants.TryResourceGroupPrefix, region.Replace(" ", Constants.TryResourceGroupSeparator), guid))
                {
                    Tags = new Dictionary<string, string> 
                    {
                        { Constants.StartTime, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
                        { Constants.IsRbacEnabled, false.ToString() },
                        { Constants.GeoRegion, region },
                        { Constants.IsExtended, false.ToString() }
                    }
                };

            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, ArmUriTemplates.ResourceGroup.Bind(resourceGroup), new
            {
                tags = resourceGroup.Tags,
                properties = new { },
                location = region
            });
            await csmResponse.EnsureSuccessStatusCodeWithFullError();

            return resourceGroup;
        }

        public static async Task<ResourceGroup> Delete(this ResourceGroup resourceGroup, bool block)
        {
            // Mark as a "Bad" resourceGroup just in case the delete fails for any reason.
            // Also since this is a potentially bad resourceGroup, ignore failure
            resourceGroup.Tags["Bad"] = "1";
            await Update(resourceGroup).IgnoreFailure();

            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Delete, ArmUriTemplates.ResourceGroup.Bind(resourceGroup));
            await csmResponse.EnsureSuccessStatusCodeWithFullError();
            if (block)
            {
                var location = csmResponse.Headers.Location;
                if (location != null)
                {
                    var deleted = false;
                    do
                    {
                        var response = await csmClient.HttpInvoke(HttpMethod.Get, location);
                        await response.EnsureSuccessStatusCodeWithFullError();

                        //TODO: How does this handle failing to delete a resourceGroup?
                        if (response.StatusCode == HttpStatusCode.OK ||
                            response.StatusCode == HttpStatusCode.NoContent)
                        {
                            deleted = true;
                        }
                        else
                        {
                            await Task.Delay(5000);
                        }

                    } while(!deleted);
                }
                else
                {
                    //No idea what this means from CSM
                }
            }
            return null;
        }

        public static async Task<ResourceGroup> PutInDesiredState(this ResourceGroup resourceGroup)
        {
            // If the resourceGroup is assigned, don't mess with it
            if (!string.IsNullOrEmpty(resourceGroup.UserId)) return resourceGroup;

            var createdSites = new List<Task<Site>>();
            var storageAccounts = new List<Task<StorageAccount>>();

            if (!resourceGroup.Sites.Any(s => s.IsSimpleWAWSOriginalSite))
            {
                createdSites.Add(CreateSite(resourceGroup, SiteNameGenerator.GenerateName));
            }

            // Create Functions Container Site
            if (!resourceGroup.Sites.Any(s => s.IsFunctionsContainer))
            {
                createdSites.Add(CreateSite(resourceGroup, () => $"{Constants.FunctionsSitePrefix}{Guid.NewGuid().ToString().Split('-').First()}"));
            }

            if (!resourceGroup.StorageAccounts.Any(s => s.IsFunctionsStorageAccount))
            {
                storageAccounts.Add(CreateStorageAccount(resourceGroup, () => $"{Constants.FunctionsStorageAccountPrefix}{Guid.NewGuid().ToString().Split('-').First()}".ToLowerInvariant()));
            }

            resourceGroup.Sites = resourceGroup.Sites.Union(await createdSites.WhenAll());
            resourceGroup.StorageAccounts = resourceGroup.StorageAccounts.Union(await storageAccounts.WhenAll());

            await InitApiApps(resourceGroup);


            await InitFunctionsContainer(resourceGroup);

            return resourceGroup;
        }

        public static async Task<ResourceGroup> DeleteAndCreateReplacement(this ResourceGroup resourceGroup)
        {
            var region = resourceGroup.GeoRegion;
            var subscriptionId = resourceGroup.SubscriptionId;
            await Delete(resourceGroup, block: false);
            return await PutInDesiredState(await CreateResourceGroup(subscriptionId, region));
        }

        public static async Task<ResourceGroup> MarkInUse(this ResourceGroup resourceGroup, string userId, AppService appService)
        {
            resourceGroup.Tags[Constants.UserId] = userId;
            resourceGroup.Tags[Constants.StartTime] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            resourceGroup.Tags[Constants.LifeTimeInMinutes] = ResourceGroup.DefaultUsageTimeSpan.TotalMinutes.ToString();
            resourceGroup.Tags[Constants.AppService] = appService.ToString();
            return await Update(resourceGroup);
        }

        public static async Task<ResourceGroup> ExtendExpirationTime(this ResourceGroup resourceGroup)
        {
            resourceGroup.Tags[Constants.StartTime] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            resourceGroup.Tags[Constants.IsExtended] = true.ToString();
            resourceGroup.Tags[Constants.LifeTimeInMinutes] = ResourceGroup.ExtendedUsageTimeSpan.TotalMinutes.ToString();
            var site = resourceGroup.Sites.FirstOrDefault(s => s.IsSimpleWAWSOriginalSite);
            var siteTask = Task.FromResult(site);
            if (site != null)
            {
                site.AppSettings["LAST_MODIFIED_TIME_UTC"] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
                site.AppSettings["SITE_LIFE_TIME_IN_MINUTES"] = ResourceGroup.ExtendedUsageTimeSpan.TotalMinutes.ToString();
                siteTask = site.UpdateAppSettings();
            }
            var resourceGroupTask = Update(resourceGroup);
            await Task.WhenAll(siteTask, resourceGroupTask);
            return resourceGroupTask.Result;
        }

        public static async Task<bool> AddResourceGroupRbac(this ResourceGroup resourceGroup, string puidOrAltSec, string emailAddress, bool isFunctionContainer = false)
        {
            try
            {
                var objectId = await GetUserObjectId(puidOrAltSec, emailAddress);

                if (string.IsNullOrEmpty(objectId)) return false;

                return (await
                    new[] { resourceGroup.AddRbacAccess(objectId) }
                    .Concat(resourceGroup.Sites.Where(s => !s.IsFunctionsContainer || isFunctionContainer).Select(s => s.AddRbacAccess(objectId)))
                    .Concat(resourceGroup.ApiApps.Select(s => s.AddRbacAccess(objectId)))
                    .Concat(resourceGroup.Gateways.Select(s => s.AddRbacAccess(objectId)))
                    .Concat(resourceGroup.LogicApps.Select(s => s.AddRbacAccess(objectId)))
                    .Concat(resourceGroup.ServerFarms.Select(s => s.AddRbacAccess(objectId)))
                    .Concat(resourceGroup.StorageAccounts.Where(s => isFunctionContainer).Select(s => s.AddRbacAccess(objectId)))
                    .WhenAll())
                    .All(e => e);
            }
            catch
            {
                return false;
            }
        }

        private static async Task<Site> CreateSite(ResourceGroup resourceGroup, Func<string> nameGenerator)
        {
            var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator());
            var csmSiteResponse = await csmClient.HttpInvoke(HttpMethod.Put, ArmUriTemplates.Site.Bind(site), new { properties = new { }, location = resourceGroup.GeoRegion });
            await csmSiteResponse.EnsureSuccessStatusCodeWithFullError();

            var csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();

            return await Load(site, csmSite);
        }

        private static bool IsSimpleWaws(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return !string.IsNullOrEmpty(csmResourceGroup.name) &&
                csmResourceGroup.name.StartsWith(Constants.TryResourceGroupPrefix, StringComparison.OrdinalIgnoreCase) &&
                csmResourceGroup.properties.provisioningState == "Succeeded" &&
                !csmResourceGroup.tags.ContainsKey("Bad");
        }

        private static async Task InitApiApps(ResourceGroup resourceGroup)
        {
            if (!resourceGroup.Tags.ContainsKey(Constants.CommonApiAppsDeployed) ||
                !resourceGroup.Tags[Constants.CommonApiAppsDeployed].Equals(Constants.CommonApiAppsDeployedVersion))
            {
                var csmTemplateString = string.Empty;

                using (var reader = new StreamReader(SimpleSettings.CommonApiAppsCsmTemplatePath))
                {
                    csmTemplateString = await reader.ReadToEndAsync();
                }

                var gatewayName = resourceGroup.Gateways.Count() != 0
                    ? resourceGroup.Gateways.Select(s => s.GatewayName).First()
                    : Guid.NewGuid().ToString().Replace("-", "");
                csmTemplateString = csmTemplateString.Replace("{{gatewayName}}", gatewayName);

                var deployment = new CsmDeployment
                {
                    DeploymentName = resourceGroup.ResourceUniqueId,
                    SubscriptionId = resourceGroup.SubscriptionId,
                    ResourceGroupName = resourceGroup.ResourceGroupName,
                    CsmTemplate = JsonConvert.DeserializeObject<JToken>(csmTemplateString),
                };

                await RetryHelper.Retry(() => deployment.Deploy(block: true), 3);
                resourceGroup.Tags[Constants.CommonApiAppsDeployed] = Constants.CommonApiAppsDeployedVersion;
                await resourceGroup.Update();
                await resourceGroup.Load();
            }
        }

        private static async Task InitFunctionsContainer(ResourceGroup resourceGroup)
        {
            var functionContainer = resourceGroup.Sites.FirstOrDefault(s => s.IsFunctionsContainer);
            var functionsStorageAccount = resourceGroup.StorageAccounts.FirstOrDefault(s => s.IsFunctionsStorageAccount);

            if (functionContainer == null || functionsStorageAccount == null) return; // This should throw some kind of error? maybe?
            if (!resourceGroup.Tags.ContainsKey(Constants.FunctionsContainerDeployed) ||
                !resourceGroup.Tags[Constants.FunctionsContainerDeployed].Equals(Constants.FunctionsContainerDeployedVersion) ||
                !functionContainer.AppSettings.ContainsKey(Constants.SiteExtensionsVersion) ||
                !functionContainer.AppSettings.ContainsKey(Constants.CurrentSiteExtensionsVersion))
            {
                await Task.WhenAll(CreateHostJson(functionContainer), CreateSecretsForFunctionsContainer(functionContainer));
                await PublishCustomSiteExtensions(functionContainer);
                await UpdateConfig(functionContainer, new { properties = new { scmType = "LocalGit" } });
                resourceGroup.Tags[Constants.FunctionsContainerDeployed] = Constants.FunctionsContainerDeployedVersion;
                await resourceGroup.Update();
                await resourceGroup.Load();
            }

            if (!functionContainer.AppSettings.ContainsKey(Constants.AzureStorageAppSettingsName))
            {
                await LinkSiteAndStorageAccount(functionContainer, functionsStorageAccount);
            }
        }

        private static async Task LinkSiteAndStorageAccount(Site site, StorageAccount storageAccount)
        {
            // Assumes site and storage are loaded
            site.AppSettings[Constants.AzureStorageAppSettingsName] = string.Format(Constants.StorageConnectionStringTemplate, storageAccount.StorageAccountName, storageAccount.StorageAccountKey);
            site.AppSettings[Constants.AzureStorageDashboardAppSettingsName] = string.Format(Constants.StorageConnectionStringTemplate, storageAccount.StorageAccountName, storageAccount.StorageAccountKey);
            await UpdateAppSettings(site);
        }

        private static async Task<StorageAccount> CreateStorageAccount(ResourceGroup resourceGroup, Func<string> nameGenerator)
        {
            var storageAccount = new StorageAccount(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator());
            var csmStorageResponse = await csmClient.HttpInvoke(HttpMethod.Put, ArmUriTemplates.StorageAccount.Bind(storageAccount), new { properties = new { accountType = "Standard_LRS" }, location = resourceGroup.GeoRegion });
            await csmStorageResponse.EnsureSuccessStatusCodeWithFullError();

            var csmStorageAccount = await WaitUntilReady(storageAccount);
            storageAccount = await Load(storageAccount, csmStorageAccount);
            await storageAccount.EnableStorageAnalytics();
            return storageAccount;
        }
    }
}