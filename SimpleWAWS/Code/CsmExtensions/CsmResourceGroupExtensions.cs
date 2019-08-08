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
using Microsoft.ApplicationInsights;
using Kudu.Client.Zip;
using Kudu.Client.Editor;
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
                var csmResourceGroupResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.ResourceGroup.Bind(resourceGroup));
                await csmResourceGroupResponse.EnsureSuccessStatusCodeWithFullError();
                csmResourceGroup = await csmResourceGroupResponse.Content.ReadAsAsync<CsmWrapper<CsmResourceGroup>>();
            }

                //Not sure what to do at this point TODO
                Validate.NotNull(csmResourceGroup.tags, "csmResourcegroup.tags");

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
                    await Task.WhenAll(LoadSites(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/sites", StringComparison.OrdinalIgnoreCase)))
                                   //LoadServerFarms(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/serverFarms", StringComparison.OrdinalIgnoreCase))),
                                   //LoadStorageAccounts(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Storage/storageAccounts", StringComparison.OrdinalIgnoreCase)))
                                   );
                }
                else if (resourceGroup.SubscriptionType == SubscriptionType.Linux)
                {
                    await Task.WhenAll(LoadLinuxResources(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/sites", StringComparison.OrdinalIgnoreCase))));
                }
                else if (resourceGroup.SubscriptionType == SubscriptionType.VSCodeLinux)
                {
                    await Task.WhenAll(LoadVSCodeLinuxResources(resourceGroup, resources.Where(r => r.type.Equals("Microsoft.Web/sites", StringComparison.OrdinalIgnoreCase))));
                }
            }

            return resourceGroup;
        }

        // Full Site Load
        public static async Task<ResourceGroup> LoadSites(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> sites = null)
        {
            try
            {
                var csmSitesResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.Sites.Bind(resourceGroup));
                await csmSitesResponse.EnsureSuccessStatusCodeWithFullError();

                var csmSites = await csmSitesResponse.Content.ReadAsAsync<CsmArrayWrapper<CsmSite>>();
                //TODO: At least log if somehow there were more than one site in there 
                resourceGroup.Site = (await csmSites.value.Select(async cs => await Load(new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, cs.name, cs.kind), cs)).WhenAll()).FirstOrDefault(s => s.IsSimpleWAWSOriginalSite);

            }
            catch (Exception ex)
            {
                // Set up some properties:
                var properties = new Dictionary<string, string>
                {{"RGName", resourceGroup?.ResourceGroupName},
                { "SubId", resourceGroup?.SubscriptionId},
                { "SubType", resourceGroup?.SubscriptionType.ToString()}};

                AppInsights.TelemetryClient.TrackException(ex, properties, null);
            }
            return resourceGroup;
        }


        public static async Task<ResourceGroup> LoadLinuxResources(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> sites = null)
        {
            return await LoadSites(resourceGroup, sites);
        }
        public static async Task<ResourceGroup> LoadVSCodeLinuxResources(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> sites = null)
        {
            return await LoadSites(resourceGroup, sites);
        }
        public static async Task<ResourceGroup> LoadMonitoringToolResources(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> sites = null)
        {
            return await LoadSites(resourceGroup, sites);
        }
        //Shallow load
        //public static async Task<ResourceGroup> LoadServerFarms(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> serverFarms = null)
        //{
        //    if (serverFarms == null)
        //    {
        //        var csmServerFarmsResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.ServerFarms.Bind(resourceGroup));
        //        await csmServerFarmsResponse.EnsureSuccessStatusCodeWithFullError();
        //        serverFarms = (await csmServerFarmsResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
        //    }
        //    resourceGroup.ServerFarms = serverFarms.Select(s => new ServerFarm(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, s.name, resourceGroup.GeoRegion));

        //    return resourceGroup;
        //}

        //public static async Task<ResourceGroup> LoadStorageAccounts(this ResourceGroup resourceGroup, IEnumerable<CsmWrapper<object>> storageAccounts = null)
        //{
        //    if (storageAccounts == null)
        //    {
        //        var csmStorageAccountsResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.StorageAccounts.Bind(resourceGroup));
        //        await csmStorageAccountsResponse.EnsureSuccessStatusCodeWithFullError();
        //        storageAccounts = (await csmStorageAccountsResponse.Content.ReadAsAsync<CsmArrayWrapper<object>>()).value;
        //    }

        //    resourceGroup.StorageAccounts = await storageAccounts.Select(async s => await Load(new StorageAccount(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, s.name), null)).WhenAll();
        //    return resourceGroup;
        //}

        public static async Task<ResourceGroup> Update(this ResourceGroup resourceGroup)
        {
            var csmResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.ResourceGroup.Bind(resourceGroup), new { properties = new { }, tags = resourceGroup.Tags, location = resourceGroup.GeoRegion });
            await csmResponse.EnsureSuccessStatusCodeWithFullError();
            return resourceGroup;
        }

        public static async Task<ResourceGroup> CreateResourceGroup(string subscriptionId, string region, string templateName)
        {
            var guid = Guid.NewGuid().ToString("N").Substring(0,24);

            var rgName = string.IsNullOrEmpty(templateName) ? String.Empty : templateName.ToString().Trim().Replace(" ", Constants.TryResourceGroupSeparator).Replace(".", Constants.TryResourceGroupSeparator).ToLowerInvariant();

            var resourceGroup = new ResourceGroup(subscriptionId, string.Join(Constants.TryResourceGroupSeparator, Constants.TryResourceGroupPrefix, rgName, guid), templateName)
            {
                Tags = new Dictionary<string, string>
                    {
                        { Constants.StartTime, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
                        { Constants.IsRbacEnabled, false.ToString() },
                        { Constants.GeoRegion, region },
                        { Constants.IsExtended, false.ToString() },
                        { Constants.SubscriptionType, (new Subscription(subscriptionId)).Type.ToString()},
                        { Constants.TemplateName, templateName??String.Empty}
                    }
            };

            var csmResponse = await GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.ResourceGroup.Bind(resourceGroup), new
            {
                tags = resourceGroup.Tags,
                properties = new { },
                location = region
            });
            await csmResponse.EnsureSuccessStatusCodeWithFullError();
            resourceGroup.TemplateName = templateName;
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
            // If the resourceGroup is assigned, don't mess with it
            if (!string.IsNullOrEmpty(resourceGroup.UserId))
            {
                return resourceGroup;
            }

            if (resourceGroup.Site ==null  || String.IsNullOrEmpty(resourceGroup.SiteGuid))
            {
                 resourceGroup.Site  = await CreateVSCodeLinuxSite(resourceGroup, SiteNameGenerator.GenerateLinuxSiteName) ;
            }
        return resourceGroup;
        }

        public static async Task<ResourceGroup> DeleteAndCreateReplacement(this ResourceGroup resourceGroup, bool blockDelete = false)
        {
            var region = resourceGroup.GeoRegion;
            var templateName = resourceGroup.TemplateName;
            var subscriptionId = resourceGroup.SubscriptionId;
            await Delete(resourceGroup, block: blockDelete);
            //TODO: add a check here to only create resourcegroup if the quota per sub/region is not met.
            return await PutInDesiredState(await CreateResourceGroup(subscriptionId, region, templateName));
        }

        public static async Task<ResourceGroup> MarkInUse(this ResourceGroup resourceGroup, string userId, AppService appService)
        {
            resourceGroup.Tags[Constants.UserId] = userId.Substring(0, Math.Min(userId.Length, 256));
            resourceGroup.Tags[Constants.UserId2] = userId.Length>256?userId.Substring(256, Math.Min(userId.Length-256, 256)):String.Empty;
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
            var site = resourceGroup.Site;
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


        //private static async Task<Site> CreateSite(ResourceGroup resourceGroup, Func<string> nameGenerator, string siteKind = null)
        //{
        //    var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator(), siteKind);
        //    await resourceGroup.LoadServerFarms(serverFarms: null);
        //    var serverFarm = new ServerFarm(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName,
        //        Constants.DefaultServerFarmName, resourceGroup.GeoRegion);
        //    if (resourceGroup.ServerFarms.Count() < 1) {
        //        var csmServerFarmResponse =
        //            await
        //                GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.ServerFarmCreate.Bind(serverFarm),
        //                    new
        //                    {
        //                        location = resourceGroup.GeoRegion,
        //                        kind = "app",
        //                        name = serverFarm.ServerFarmName,
        //                        sku= serverFarm.Sku,
        //                        properties = new
        //                        {
        //                            name = serverFarm.ServerFarmName,
        //                            workerSizeId=0,
        //                            numberOfWorkers= 0,
        //                            geoRegion=resourceGroup.GeoRegion,
        //                            kind="app"
        //                        }
        //                    });
        //        await csmServerFarmResponse.EnsureSuccessStatusCodeWithFullError();
        //    }

        //    var csmSiteResponse =
        //        await
        //            GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Put, ArmUriTemplates.SiteCreate.Bind(site),
        //                new
        //                {
        //                    properties =
        //                        new
        //                        {
        //                            serverFarmId = serverFarm.CsmId
        //                        },
        //                    location = resourceGroup.GeoRegion,
        //                    kind = "app",
        //                    name = site.SiteName
        //                });
        //    await csmSiteResponse.EnsureSuccessStatusCodeWithFullError();
        //    var csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();

        //    return await Load(site, csmSite);
        //}

        private static async Task<Site> CreateLinuxSite(ResourceGroup resourceGroup, Func<string> nameGenerator, string siteKind = null)
        {
            var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator(), siteKind);
            var csmTemplateString = string.Empty;
            var template = TemplatesManager.GetTemplates().FirstOrDefault(t => t.Name == Constants.NodejsWebAppLinuxTemplateName) as LinuxTemplate;

            using (var reader = new StreamReader(template.CsmTemplateFilePath))
            {
                csmTemplateString = await reader.ReadToEndAsync();
            }

            csmTemplateString = csmTemplateString
                                .Replace("{{siteName}}", site.SiteName)
                                .Replace("{{aspName}}", site.SiteName + "-plan")
                                .Replace("{{vmLocation}}", resourceGroup.GeoRegion)
                                .Replace("{{serverFarmType}}", SimpleSettings.ServerFarmTypeContent);
            var inProgressOperation = new InProgressOperation(resourceGroup, DeploymentType.CsmDeploy);
            await inProgressOperation.CreateDeployment(JsonConvert.DeserializeObject<JToken>(csmTemplateString), block: true, subscriptionType: resourceGroup.SubscriptionType);
 
            // Dont run this yet. Spot serverfarms clock will start 
            //await Util.DeployLinuxTemplateToSite(template, site);

            if (!resourceGroup.Tags.ContainsKey(Constants.LinuxAppDeployed))
            {
                resourceGroup.Tags.Add(Constants.LinuxAppDeployed, "1");
                await resourceGroup.Update();
            }
            await Load(site, null);
            return site;
        }

        private static async Task<string> GetConfig(string url)
        {
            var s = String.Empty;
            try
            {
                using (WebClient client = new WebClient())
                {
                    // Add a user agent header in case the 
                    // requested URI contains a query.

                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

                    using (Stream data =  client.OpenRead(new Uri(url)))
                    {
                        using (StreamReader reader = new StreamReader(data))
                        {
                            s = await reader.ReadToEndAsync();
                            reader.Close();
                        }
                        data.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleTrace.TraceException(ex);
            }
            return s;
        }

        private static async Task<Site> CreateVSCodeLinuxSite(ResourceGroup resourceGroup, Func<string> nameGenerator, string siteKind = null)
        {
            var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var site = new Site(resourceGroup.SubscriptionId, resourceGroup.ResourceGroupName, nameGenerator(), siteKind);
            var csmTemplateString = string.Empty;
            var template = TemplatesManager.GetTemplates().FirstOrDefault(t => t.Name == resourceGroup.TemplateName);

            SimpleTrace.TraceInformation($"Deploying {template.Name} to {site.SiteName}->{resourceGroup.ResourceGroupName}->{resourceGroup.SubscriptionId}");

            csmTemplateString = await GetConfig(template.ARMTemplateLink);

            csmTemplateString = csmTemplateString
                                .Replace("{{appServiceName}}", site.SiteName)
                                .Replace("{{msdeployPackageUrl}}", template.MSDeployPackageUrl)
                                .Replace("{{serverFarmType}}", SimpleSettings.ServerFarmTypeContent)
                                .Replace("{{tryAppserviceUrl}}", SimpleSettings.TryAppServiceSite);

            var inProgressOperation = new InProgressOperation(resourceGroup, DeploymentType.CsmDeploy);
            var token = await inProgressOperation.CreateDeployment(JsonConvert.DeserializeObject<JToken>(csmTemplateString), block: true, subscriptionType: resourceGroup.SubscriptionType);

            SimpleTrace.TraceInformation($"ARM Deployment result: {JsonConvert.SerializeObject(token)} to {site.SiteName}->{resourceGroup.ResourceGroupName}->{resourceGroup.SubscriptionId}");
            var csmSiteResponse =
                await
                    GetClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.SiteCreate.Bind(site));
            await csmSiteResponse.EnsureSuccessStatusCodeWithFullError();
            var csmSite = await csmSiteResponse.Content.ReadAsAsync<CsmWrapper<CsmSite>>();

            await Load(site, csmSite);
            SimpleTrace.TraceInformation($"Site Loaded from ARM : {JsonConvert.SerializeObject(site)} to {site.SiteName}->{resourceGroup.ResourceGroupName}->{resourceGroup.SubscriptionId}");

            var siteguid = await Util.UpdatePostDeployAppSettings(site);
            SimpleTrace.TraceInformation($"Site AppSettings Updated:  for {site.SiteName}->{resourceGroup.ResourceGroupName}->{resourceGroup.SubscriptionId}");

            if (resourceGroup.AppService == AppService.VSCodeLinux)
            {
                await Task.Delay(30 * 1000);
                try
                {
                    var lsm = new LinuxSiteManager.Client.LinuxSiteManager(retryCount: 30);
                    await lsm.CheckSiteDeploymentStatusAsync(site.HttpUrl);
                }
                catch (Exception ex)
                {
                    SimpleTrace.TraceError($"Unable to ping deployed site {site.HostName}. Continuing regardless {ex.Message}->{ex.StackTrace} ");
                }
            }
            if (!resourceGroup.Tags.ContainsKey(Constants.TemplateName))
            {
                resourceGroup.Tags.Add(Constants.TemplateName, resourceGroup.TemplateName);
            }
            if (resourceGroup.SubscriptionType == SubscriptionType.AppService)
            {
                if (template != null && template.FileName != null)
                {
                    var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                    var vfsSCMManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);
                    Task scmRedirectUpload = vfsSCMManager.Put("site/applicationHost.xdt", Path.Combine(HostingEnvironment.MapPath(@"~/App_Data"), "applicationHost.xdt"));
                    var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);

                    await Task.WhenAll(scmRedirectUpload);
                }
            }
            if (template.Name.Equals("WordPress", StringComparison.OrdinalIgnoreCase))
            {
                await site.UpdateConfig(new { properties = new { scmType = "LocalGit", httpLoggingEnabled = true, localMySqlEnabled = true } });
            }
            resourceGroup.Tags[Constants.TemplateName] = template.Name;
            site.SubscriptionId = resourceGroup.SubscriptionId;
            //site.AppSettings = new Dictionary<string, string>();
            resourceGroup.Tags.Add(Constants.SiteGuid, siteguid);

            await Task.WhenAll(resourceGroup.Update());

            SimpleTrace.TraceInformation($"ResourceGroup Templates Tag Updated: with SiteGuid: {siteguid} for {site.SiteName} ->{resourceGroup.ResourceGroupName}->{resourceGroup.SubscriptionId}");
            return site;
        }
        

        private static bool IsSimpleWawsResourceName(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            return !string.IsNullOrEmpty(csmResourceGroup.name) &&
                csmResourceGroup.name.StartsWith(Constants.TryResourceGroupPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSimpleWawsResourceActive(CsmWrapper<CsmResourceGroup> csmResourceGroup)
        {
            try
            {
                return IsSimpleWawsResourceName(csmResourceGroup) &&
                    (csmResourceGroup.tags.ContainsKey(Constants.UserId) || csmResourceGroup.tags.ContainsKey(Constants.UserId2))
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


        //private static bool IsValidResource(CsmWrapper<CsmResourceGroup> csmResourceGroup, SubscriptionType subType)
        //{
        //    return IsSimpleWawsResourceName(csmResourceGroup) &&
        //        csmResourceGroup.properties.provisioningState == "Succeeded" &&
        //        csmResourceGroup.tags != null && !csmResourceGroup.tags.ContainsKey("Bad")
        //        && csmResourceGroup.tags.ContainsKey(Constants.SubscriptionType)
        //        && (string.Equals(csmResourceGroup.tags[Constants.SubscriptionType], subType.ToString(), StringComparison.OrdinalIgnoreCase)
        //        && csmResourceGroup.tags.ContainsKey(Constants.TemplateName));
        //}

    }
}