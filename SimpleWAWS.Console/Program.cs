using SimpleWAWS.Models;
using SimpleWAWS.Code;
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Trace;
using Serilog;
namespace SimpleWAWS.Console
{
    static class Program
    {
        private static Hashtable georegionHashMap =new Hashtable();
        static void Main(string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 12 * SimpleSettings.NUMBER_OF_PROCESSORS;
            var log = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();
            SimpleTrace.Diagnostics = log;
            SimpleTrace.Analytics = log;
            var arg = args.Length > 0 ? args[0] : String.Empty;
            switch (arg)
            {
                case "fixfunctionssetting":
                    Task.Run(() => FixFunctionsSetting()).Wait();
                    break;
                case "listresourcegroups":
                    Task.Run(() => ListResourceGroups()).Wait();
                    break;
                default:
                    Task.Run(() => MainAsync()).Wait();
                    break;

            }

        }

        public static void PrettyPrint(this ResourceGroup e)
        {
            Action<object> console = (s) => System.Console.WriteLine("[" + DateTime.UtcNow + "] " + s);
            console(string.Format("RG: {0} has {1} sites, named: {2}",
                e.ResourceGroupName,
                e.Sites.Count(),
                e.Sites.Count() > 0
                ? e.Sites.Select(s => s.SiteName).Aggregate((a, b) => string.Join(",", a, b))
                : "No sites")
                );
        }

        public static async Task MainAsync()
        {
            foreach (var a in SimpleSettings.GeoRegions.Split(','))
            {
                georegionHashMap.Add(a.ToLowerInvariant().Replace(" ","") ,a);
            }
            //var subscriptionNames = System.Environment.GetEnvironmentVariable("Subscriptions").Split(',');
            var csmSubscriptions = await CsmManager.GetSubscriptionNamesToIdMap();
            var subscriptionsIds = SimpleSettings.Subscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Union(SimpleSettings.LinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                //It can be either a displayName or a subscriptionId
                .Select(s => s.Trim())
                .Where(n =>
                {
                    Guid temp;
                    return csmSubscriptions.ContainsKey(n) || Guid.TryParse(n, out temp);
                })
                .Select(sn =>
                {
                    Guid temp;
                    if (Guid.TryParse(sn, out temp)) return sn;
                    else return csmSubscriptions[sn];
                });
            //var subscriptionNames = new[] { "bd5cf6af-4d44-449e-adaf-20626ae5013e" };
            var startTime = DateTime.UtcNow;

            Action<object> console = (s) => System.Console.WriteLine("[" + (DateTime.UtcNow - startTime).TotalMilliseconds + "] " + s);
            Action<Subscription> printSub = (sub) => sub.ResourceGroups.ToList().ForEach(e =>
                console(string.Format("RG: {0} has {1} sites, named: {2}",
                e.ResourceGroupName,
                e.Sites.Count(),
                e.Sites.Count() > 0
                ? e.Sites.Select(s => s.SiteName).Aggregate((a, b) => string.Join(",", a, b))
                : "No sites")
                ));


            console("start loading subscriptions");
            console("We have " + subscriptionsIds.Count() + " subscriptions");
            //var subscriptions = await subscriptionsIds.Select(s => new Subscription(s).Load(false)).WhenAll();
            //console("done loading subscriptions");

            //console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");

            console("calling MakeTrialSubscription on all subscriptions");
            foreach (var sub in subscriptionsIds)
            {
                var s = await new Subscription(sub).Load(false);
                console($"Deleting resources in {s.Type} subscription {s.SubscriptionId}");

                var csmResourceGroups = await s.LoadResourceGroupsForSubscription();
                var deleteExtras = csmResourceGroups.value
                    .GroupBy(rg => rg.location)
                    .Select(g => new { Region = g.Key, ResourceGroups = g.Select(r => r), Count = g.Count() })
                    .Where(g => g.Count > s.ResourceGroupsPerGeoRegion)
                    .Select(g => g.ResourceGroups.Where(rg => !rg.tags.ContainsKey("UserId") ).Skip((s.ResourceGroupsPerGeoRegion)))
                    .SelectMany(i => i);

                Parallel.ForEach(deleteExtras, async (resourceGroup) =>
                {
                    try
                    {
                        var georegion = getGeoRegionfromLocation(resourceGroup.location);
                        console($"Deleting leaked {georegion} resource  {resourceGroup.name}");
                        await new ResourceGroup(s.SubscriptionId,resourceGroup.name, georegion).Delete(false);
                    }
                    catch (Exception ex)
                    {
                        console($"Extra RG Delete Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
                    }
                });
                var result = s.MakeTrialSubscription();

                Parallel.ForEach(result.ToDelete, async (resourceGroup) =>
                {
                    try
                    {
                        console($"Deleting {resourceGroup.ResourceGroupName} resource in {s.Type} subscription {s.SubscriptionId}");
                        await resourceGroup.Delete(false);
                    }
                    catch (Exception ex)
                    {
                        console($"RG Delete Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
                    }
                });
            }
            //subscriptions.ToList().ForEach(printSub);
            console("Done");
        }

        private static string getGeoRegionfromLocation(string location)
        {
            return georegionHashMap.ContainsKey(location)? georegionHashMap[location].ToString() :String.Empty;
        }

        public static async Task Main2Async()
        {
            //var subscriptionNames = System.Environment.GetEnvironmentVariable("Subscriptions").Split(',');
            var csmSubscriptions = await CsmManager.GetSubscriptionNamesToIdMap();
            var subscriptionsIds = SimpleSettings.Subscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Union(SimpleSettings.LinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                //It can be either a displayName or a subscriptionId
                .Select(s => s.Trim())
                .Where(n =>
                {
                    Guid temp;
                    return csmSubscriptions.ContainsKey(n) || Guid.TryParse(n, out temp);
                })
                .Select(sn =>
                {
                    Guid temp;
                    if (Guid.TryParse(sn, out temp)) return sn;
                    else return csmSubscriptions[sn];
                });
            //var subscriptionNames = new[] { "bd5cf6af-4d44-449e-adaf-20626ae5013e" };
            var startTime = DateTime.UtcNow;

            Action<object> console = (s) => System.Console.WriteLine("[" + (DateTime.UtcNow - startTime).TotalMilliseconds + "] " + s);
            Action<Subscription> printSub = (sub) => sub.ResourceGroups.ToList().ForEach(e => 
                console(string.Format("RG: {0} has {1} sites, named: {2}",
                e.ResourceGroupName,
                e.Sites.Count(),
                e.Sites.Count() > 0
                ? e.Sites.Select(s => s.SiteName).Aggregate((a, b) => string.Join(",", a, b))
                : "No sites")
                ));


            console("start loading subscriptions");
            console("We have " + subscriptionsIds.Count() + " subscriptions");
            var subscriptions = await subscriptionsIds.Select(s => new Subscription(s).Load(false)).WhenAll();
            console("done loading subscriptions");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");

            //foreach (var resourcegroup in subscriptions.SelectMany(subscription => subscription.ResourceGroups))
            //        {
            //            try
            //            {
            //                if (!resourcegroup.IsSimpleWAWS)
            //                {
            //                    console($" Replacing Resource Group : {resourcegroup.CsmId}");
            //                    await resourcegroup.DeleteAndCreateReplacement(true);
            //                    console($" Replaced");
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                console(ex.ToString());
            //            }

            //}

            ////console("calling MakeTrialSubscription on all subscriptions");
            ////await subscriptions.Select(async s =>
            ////{

            ////    var result = s.MakeTrialSubscription();
            ////    foreach (var resourceGroup in result.Ready)
            ////    {
            ////        try
            ////        {
            ////            await resourceGroup.PutInDesiredState();
            ////        }
            ////        catch (Exception ex)
            ////        {
            ////            console(
            ////                $"RG PIDS Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
            ////        }
            ////    }
            ////    //foreach (var geoRegion in result.ToCreateInRegions)
            ////    //{
            ////    //    try
            ////    //    {
            ////    //        for (int i=0; i < (s.Type==SubscriptionType.Linux?10: 1); i++)
            ////    //        {
            ////    //            await CsmManager.CreateResourceGroup(s.SubscriptionId, geoRegion).Result.PutInDesiredState();
            ////    //        }
            ////    //    }
            ////    //    catch (Exception ex)
            ////    //    {
            ////    //        console($"GR Create Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
            ////    //    }
            ////    //}
            ////    foreach (var resourceGroup in result.ToDelete)
            ////    {
            ////        try { await resourceGroup.Delete(true); }
            ////        catch (Exception ex)
            ////        {
            ////            console($"RG Delete Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
            ////        }
            ////    }
            ////}).WhenAll();
            console("done calling make trial subscription");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");
            console(subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()));
            console("make free trial");
            //Parallel.ForEach(
            //    subscriptions.Where(r => r.Type == SubscriptionType.AppService).SelectMany(subscription => subscription.ResourceGroups), async (resourcegroup) =>
            //    {
            //        console($"Deleting resourcegroup:{resourcegroup.ResourceGroupName} from \tsubscription:{resourcegroup.SubscriptionId}");
            //        await resourcegroup.Delete(true);
            //    });

            //console("Re-start loading subscriptions");
            //console("We have " + subscriptionsIds.Count() + " subscriptions");
            //subscriptions = await subscriptionsIds.Select(s => new Subscription(s).Load()).WhenAll();
            //console("done loading subscriptions");

            Parallel.ForEach(
                subscriptions.Where(r => r.Type == SubscriptionType.AppService  || r.Type == SubscriptionType.Linux).SelectMany(subscription => subscription.ResourceGroups), async (resourcegroup) =>
                {
                    //foreach (var resourcegroup in subscriptions.Where(r => r.Type == SubscriptionType.AppService).SelectMany(subscription => subscription.ResourceGroups))
                    //{
                    try
                {
                    console($" Deleting Resource Group : {resourcegroup.CsmId}");
                    resourcegroup.Delete(false).GetAwaiter().GetResult();//DeleteAndCreateReplacement(true);
                    console($" Deleted");
                }
                catch (Exception ex)
                {
                    console(ex.ToString());
                }

                }

                //}
                );
                //subscriptions.ToList().ForEach(printSub);
                console("Done");
        }

     
        public static async Task ListResourceGroups()
        {
            var startTime = DateTime.UtcNow;
            Action<object> console = (s) => System.Console.WriteLine("[" + (DateTime.UtcNow - startTime).TotalMilliseconds + "] " + s);

            console("start");
            var manager = await ResourcesManager.GetInstanceAsync();
            do
            {
                console("count " + manager.GetAllFreeResourceGroups().Count);
                console("count " + manager.GetAllInUseResourceGroups().Count);
                console("count " + manager.GetAllBackgroundOperations().Count);
                System.Console.ReadLine();
            } while (true);
            //console("activate api app");
            //var resourceGroup = await manager.ActivateLogicApp(new LogicTemplate
            //        {
            //            Name = "Ping Site",
            //            SpriteName = "sprite-ASPNETEmptySite ASPNETEmptySite",
            //            AppService = AppService.Logic,
            //            CsmTemplateFilePath = @"D:\scratch\repos\SimpleWAWS\SimpleWAWS\App_Data\PingSite.json"
            //        }, new Authentication.TryWebsitesIdentity("asdfsdasdsdfsdfsdf3rfsasdasd333d@test.com", null, "AAD"), "");
            //console("done activating api app");

            //resourceGroup.PrettyPrint();

        }

        public static async Task FixFunctionsSetting()
        {
            var startTime = DateTime.UtcNow;
            Action<object> console = (s) => System.Console.WriteLine("[" + (DateTime.UtcNow - startTime).TotalMilliseconds + "] " + s);

            console("start");
            var csmSubscriptions = await CsmManager.GetSubscriptionNamesToIdMap();
            var subscriptionsIds = SimpleSettings.Subscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                //It can be either a displayName or a subscriptionId
                .Select(s => s.Trim())
                .Where(n =>
                {
                    Guid temp;
                    return csmSubscriptions.ContainsKey(n) || Guid.TryParse(n, out temp);
                })
                .Select(sn =>
                {
                    Guid temp;
                    if (Guid.TryParse(sn, out temp)) return sn;
                    else return csmSubscriptions[sn];
                });


            console("start loading subscriptions");
            console("We have " + subscriptionsIds.Count() + " subscriptions");
            var subscriptions = await subscriptionsIds.Select(s => new Subscription(s).Load()).WhenAll();
            console("done loading subscriptions");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");

            foreach (var resourcegroup in subscriptions.SelectMany(subscription => subscription.ResourceGroups))
            {
                try
                {
                    console($" Updating Resource Group : {resourcegroup.CsmId}");
                    await resourcegroup.Load();
                    var site = resourcegroup.Sites.First(s => s.IsSimpleWAWSOriginalSite);

                    if (site != null && site.AppSettings.ContainsKey("FUNCTIONS_EXTENSION_VERSION"))
                    {
                        site.AppSettings.Remove("FUNCTIONS_EXTENSION_VERSION");
                    }
                    await site.UpdateAppSettings();
                    site = resourcegroup.Sites.First(s => !s.IsSimpleWAWSOriginalSite);
                    if (site != null && site.AppSettings.ContainsKey("FUNCTIONS_EXTENSION_VERSION") 
                        && !string.Equals( site.AppSettings["FUNCTIONS_EXTENSION_VERSION"], SimpleSettings.FunctionsExtensionVersion))
                    {
                        site.AppSettings["FUNCTIONS_EXTENSION_VERSION"] = SimpleSettings.FunctionsExtensionVersion;
                    }
                    await site.UpdateAppSettings();
                    console($" Updated");
                }
                catch (Exception ex)
                {
                    console(ex.ToString());
                }

            }


        }
    }
}
