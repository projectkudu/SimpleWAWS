using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<ResourceGroup> Load(this ResourceGroup resourceGroup, CsmWrapper<CsmResourceGroup> csmResourceGroup = null, IEnumerable<CsmWrapper<object>> resources = null, bool loadSubResources = true)
        {
            Validate.ValidateCsmResourceGroup(resourceGroup);

            if (csmResourceGroup == null)
            {
                var csmResourceGroupResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroup.Bind(resourceGroup));
                await csmResourceGroupResponse.EnsureSuccessStatusCodeWithFullError();
                csmResourceGroup = await csmResourceGroupResponse.Content.ReadAsAsync<CsmWrapper<CsmResourceGroup>>();
            }

            //Not sure what to do at this point TODO
            Validate.NotNull(csmResourceGroup.tags, "csmResorucegroup.tags");

            resourceGroup.Tags = csmResourceGroup.tags;

            if (resources == null)
            {
                var csmResourceGroupResourcesResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroupResources.Bind(resourceGroup));
                await csmResourceGroupResourcesResponse.EnsureSuccessStatusCodeWithFullError();
                resources = (await csmResourceGroupResourcesResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
            }

            if (loadSubResources)
            {
                if (resourceGroup.SubscriptionType == SubscriptionType.AppService)
                {
                    await Task.WhenAll(LoadSites(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/sites", StringComparison.OrdinalIgnoreCase))),
                                   LoadLogicApps(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Logic/workflows", StringComparison.OrdinalIgnoreCase))),
                                   LoadServerFarms(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/serverFarms", StringComparison.OrdinalIgnoreCase))),
                                   LoadStorageAccounts(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Storage/storageAccounts", StringComparison.OrdinalIgnoreCase))));
                }
                else
                {
                    await Task.WhenAll(LoadLinuxResources(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/sites", StringComparison.OrdinalIgnoreCase))));
                }
            }

            return resourceGroup;
        }

        // Full Site Load
        public static async Task<ResourceGroup> LoadSites(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> sites = null)
        {
            var csmSitesResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.Sites.Bind(resourceGroup));
            await csmSitesResponse.EnsureSuccessStatusCodeWithFullError();

            var csmSites = await csmSitesResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmSite>>();
            resourceGroup.Sites = await csmSites.value.Select(async cs => await Load(new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, cs.name, cs.kind), cs)).WhenAll();
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

        public static async Task<ResourceGroup> LoadLinuxResources(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> sites = null)
        {
            return await LoadSites(resourceGroup,sites);
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
            resourceGroup.ServerFarms = serverFarms.Select(s => new ServerFarm(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, s.name, resourceGroup.GeoRegion));

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
            var csmResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.ResourceGroup.Bind(resourceGroup), new { properties = new { }, tags = resourceGroup.Tags, location = resourceGroup.GeoRegion });
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
                        { Constants.IsExtended, false.ToString() },
                        { Constants.SubscriptionType, (new Subscription(subscriptionId)).Type.ToString()}
                    }
            };

            var csmResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.ResourceGroup.Bind(resourceGroup), new
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

            var csmResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Delete, ArmUriTemplates.ResourceGroup.Bind(resourceGroup));
            await csmResponse.EnsureSuccessStatusCodeWithFullError();
            if (block)
            {
                var location = csmResponse.Headers.Location;
                if (location != null)
                {
                    var deleted = false;
                    do
                    {
                        var response = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, location);
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

                    } while (!deleted);
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
            if (resourceGroup.SubscriptionType == SubscriptionType.AppService)
            {
                // If the resourceGroup is assigned, don't mess with it
                if (!string.IsNullOrEmpty(resourceGroup.UserId)) return resourceGroup;

                var createdSites = new List<Task<Site>>();

                if (!resourceGroup.Sites.Any(s => s.IsSimpleWAWSOriginalSite))
                {
                    createdSites.Add(CreateSite(resourceGroup, SiteNameGenerator.GenerateName));
                }

                resourceGroup.Sites = resourceGroup.Sites.Union(await createdSites.WhenAll());

                // Create Functions Container Site
                if (!resourceGroup.Sites.Any(s => s.IsFunctionsContainer))
                {
                    createdSites.Add(CreateFunctionApp(resourceGroup,
                        (() => $"{Constants.FunctionsSitePrefix}{Guid.NewGuid().ToString().Split('-').First()}"),
                        "functionapp"));
                }

                resourceGroup.Sites = resourceGroup.Sites.Union(await createdSites.WhenAll());

                await InitFunctionsContainer(resourceGroup);
            }
            else if (resourceGroup.SubscriptionType == SubscriptionType.Linux)
            {
                // If the resourceGroup is assigned, don't mess with it
                if (!string.IsNullOrEmpty(resourceGroup.UserId))
                {
                    return resourceGroup;
                }

                if (!resourceGroup.Sites.Any(s => s.IsSimpleWAWSOriginalSite))
                {
                        resourceGroup.Sites = new List<Site> {(await CreateLinuxSite(resourceGroup, SiteNameGenerator.GenerateName))};
                }

            }
            return resourceGroup;
        }

        public static async Task<ResourceGroup> DeleteAndCreateReplacement(this ResourceGroup resourceGroup, bool blockDelete = false)
        {
            var region = resourceGroup.GeoRegion;
            var subscriptionId = resourceGroup.SubscriptionId;
            await Delete(resourceGroup, block: blockDelete);
            //TODO: add a check here to only create resourcegroup if the quota per sub/region is not met.
            return await PutInDesiredState(await CreateResourceGroup(subscriptionId, region));
        }

        public static async Task<ResourceGroup> MarkInUse(this ResourceGroup resourceGroup, string userId, AppService appService)
        {
            resourceGroup.Tags[Constants.UserId] = userId;
            resourceGroup.Tags[Constants.StartTime] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            switch (appService)
            {
                case AppService.Linux:
                    resourceGroup.Tags[Constants.LifeTimeInMinutes] = ResourceGroup.LinuxUsageTimeSpan.TotalMinutes.ToString();
                    break;
                default:
                    resourceGroup.Tags[Constants.LifeTimeInMinutes] = ResourceGroup.DefaultUsageTimeSpan.TotalMinutes.ToString();
                    break;
            }
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
                    .Concat(resourceGroup.ServerFarms.Select(s => s.AddRbacAccess(objectId)))
                    .Concat(resourceGroup.StorageAccounts.Where(s => isFunctionContainer).Select(s => s.AddRbacAccess(objectId)))
                    .Concat(resourceGroup.LogicApps.Select(s => s.AddRbacAccess(objectId)))
                    .WhenAll())
                    .All(e => e);
            }
            catch
            {
                return false;
            }
        }

        private static async Task<Site> CreateSite(ResourceGroup resourceGroup, Func<string> nameGenerator, string siteKind = null)
        {
            var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator(), siteKind);
            await resourceGroup.LoadServerFarms(serverFarms: null);
            var serverFarm = new ServerFarm(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName,
                Constants.DefaultServerFarmName, resourceGroup.GeoRegion);
            var csmSiteResponse =
                await
                    csmClient.HttpInvoke(HttpMethod.Put, ArmUriTemplates.Site.Bind(site),
                        new
                        {
                            properties =
                                new
                                {
                                    serverFarm = serverFarm,
                                    sku = Constants.TryAppServiceTier
                                },
                            location = resourceGroup.GeoRegion,
                            kind = siteKind
                        });
            await csmSiteResponse.EnsureSuccessStatusCodeWithFullError();
            var csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();

            return await Load(site, csmSite);
        }

        private static async Task<Site> CreateLinuxSite(ResourceGroup resourceGroup, Func<string> nameGenerator, string siteKind = null)
        {
            var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator(), siteKind);
            var csmTemplateString = string.Empty;
            var template = TemplatesManager.GetTemplates().FirstOrDefault(t => t.Name == "Linux") as LinuxTemplate;

            using (var reader = new StreamReader(template.CsmTemplateFilePath))
            {
                csmTemplateString = await reader.ReadToEndAsync();
            }

            csmTemplateString = csmTemplateString
                                .Replace("{{siteName}}", site.SiteName)
                                .Replace("{{aspName}}", site.SiteName + "-plan")
                                .Replace("{{vmLocation}}", resourceGroup.GeoRegion);
            var inProgressOperation = new InProgressOperation(resourceGroup, DeploymentType.CsmDeploy);
            await inProgressOperation.CreateDeployment(JsonConvert.DeserializeObject<JToken>(csmTemplateString), block: true, subscriptionType: resourceGroup.SubscriptionType);
            resourceGroup.Tags.Add(Constants.LinuxAppDeployed, "1");
            await resourceGroup.Update();
            return await Load(site, null);
        }

        private static async Task<Site> CreateFunctionApp(ResourceGroup resourceGroup, Func<string> nameGenerator, string siteKind = null)
        {
            var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator(), siteKind);
            await resourceGroup.LoadServerFarms(serverFarms: null);
            ServerFarm serverFarm = new ServerFarm(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, Constants.DefaultServerFarmName,
                resourceGroup.GeoRegion);
            var csmSiteResponse =
                await
                    csmClient.HttpInvoke(HttpMethod.Put, ArmUriTemplates.FunctionsAppApiVersionTemplate.Bind(site),
                        new
                        {
                            properties =
                                new
                                {
                                    serverFarmId = serverFarm.CsmId,
                                    sku = Constants.TryAppServiceTier
                                },
                            location = resourceGroup.GeoRegion,
                            kind = siteKind
                        });
            await csmSiteResponse.EnsureSuccessStatusCodeWithFullError();
            var csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();

            return await Load(site, csmSite);
        }
        private static bool IsSimpleWaws(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return IsSimpleWawsResourceName(csmResourceGroup) &&
                csmResourceGroup.properties.provisioningState == "Succeeded" &&
                csmResourceGroup.tags != null && !csmResourceGroup.tags.ContainsKey("Bad")
                && csmResourceGroup.tags.ContainsKey(Constants.SubscriptionType)
                && string.Equals(csmResourceGroup.tags[Constants.SubscriptionType], SubscriptionType.AppService.ToString(), StringComparison.OrdinalIgnoreCase)
                && csmResourceGroup.tags.ContainsKey("FunctionsContainerDeployed");
        }
        private static bool IsSimpleWawsResourceName(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return !string.IsNullOrEmpty(csmResourceGroup.name) &&
                csmResourceGroup.name.StartsWith(Constants.TryResourceGroupPrefix, StringComparison.OrdinalIgnoreCase) ;
        }

        private static bool IsSimpleWawsResourceActive(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            try
            {
                return IsSimpleWawsResourceName(csmResourceGroup) &&
                    csmResourceGroup.tags.ContainsKey(Constants.UserId)
                    && csmResourceGroup.tags.ContainsKey(Constants.StartTime)
                    && csmResourceGroup.tags.ContainsKey(Constants.LifeTimeInMinutes)
                    && DateTime.UtcNow > DateTime.Parse(csmResourceGroup.tags[Constants.StartTime]).AddMinutes(Int32.Parse(csmResourceGroup.tags[Constants.LifeTimeInMinutes]));
            }
            catch (Exception ex)
            {
                //Assume resourcegroup is in a bad state.
                SimpleTrace.Diagnostics.Fatal("ResourceGroup in bad state {@exception}", ex);
                return false;
            }
        }

        private static bool IsLinuxResource(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return IsSimpleWawsResourceName(csmResourceGroup) &&
                csmResourceGroup.properties.provisioningState == "Succeeded" &&
                csmResourceGroup.tags != null && !csmResourceGroup.tags.ContainsKey("Bad")
                && csmResourceGroup.tags.ContainsKey(Constants.SubscriptionType)
                && string.Equals(csmResourceGroup.tags[Constants.SubscriptionType], SubscriptionType.Linux.ToString(), StringComparison.OrdinalIgnoreCase)
                && csmResourceGroup.tags.ContainsKey(Constants.LinuxAppDeployed);
        }

        private static async Task InitFunctionsContainer(ResourceGroup resourceGroup)
        {
            var functionContainer = resourceGroup.Sites.FirstOrDefault(s => s.IsFunctionsContainer);

            var storageAccounts = new List<Task<StorageAccount>>();

            if (!resourceGroup.StorageAccounts.Any(s => s.IsFunctionsStorageAccount))
            {
                storageAccounts.Add(CreateStorageAccount(resourceGroup, () => $"{resourceGroup.Sites.First((s) => s.IsFunctionsContainer).SiteName}".ToLowerInvariant()));
            }

            resourceGroup.StorageAccounts = resourceGroup.StorageAccounts.Union(await storageAccounts.WhenAll());

            var functionsStorageAccount = resourceGroup.StorageAccounts.FirstOrDefault(s => s.IsFunctionsStorageAccount);

            if (functionContainer == null || functionsStorageAccount == null) return; // This should throw some kind of error? maybe?
            if (!resourceGroup.Tags.ContainsKey(Constants.FunctionsContainerDeployed) ||
                !resourceGroup.Tags[Constants.FunctionsContainerDeployed].Equals(Constants.FunctionsContainerDeployedVersion))
            {
                await Task.WhenAll(CreateHostJson(functionContainer), CreateSecretsForFunctionsContainer(functionContainer));
                resourceGroup.Tags[Constants.FunctionsContainerDeployed] = Constants.FunctionsContainerDeployedVersion;
                await resourceGroup.Update();
                await resourceGroup.Load();
            }

            if (!functionContainer.AppSettings.ContainsKey(Constants.AzureStorageAppSettingsName))
            {
                await LinkStorageAndUpdateSettings(functionContainer, functionsStorageAccount);
            }
            //TODO: add localhost:44300 to cors allowed list here for -next slot subs
        }

        private static async Task LinkStorageAndUpdateSettings(Site site, StorageAccount storageAccount)
        {
            // Assumes site and storage are loaded
            site.AppSettings[Constants.AzureStorageAppSettingsName] = string.Format(Constants.StorageConnectionStringTemplate, storageAccount.StorageAccountName, storageAccount.StorageAccountKey);
            site.AppSettings[Constants.AzureStorageDashboardAppSettingsName] = string.Format(Constants.StorageConnectionStringTemplate, storageAccount.StorageAccountName, storageAccount.StorageAccountKey);
            if (!site.IsSimpleWAWSOriginalSite)
            {
                site.AppSettings["FUNCTIONS_EXTENSION_VERSION"] = SimpleSettings.FunctionsExtensionVersion;
            }
            site.AppSettings["WEBSITE_NODE_DEFAULT_VERSION"] = SimpleSettings.WebsiteNodeDefautlVersion;
            await UpdateAppSettings(site);
        }

        private static async Task<StorageAccount> CreateStorageAccount(ResourceGroup resourceGroup, Func<string> nameGenerator)
        {
            var storageAccount = new StorageAccount(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator());
            var csmStorageResponse = await csmClient.HttpInvoke(HttpMethod.Put, ArmUriTemplates.StorageAccount.Bind(storageAccount), new { properties = new { accountType = "Standard_LRS" }, location = resourceGroup.GeoRegion });
            await csmStorageResponse.EnsureSuccessStatusCodeWithFullError();

            var csmStorageAccount = await WaitUntilReady(storageAccount);
            return await Load(storageAccount, csmStorageAccount);
        }
    }
}