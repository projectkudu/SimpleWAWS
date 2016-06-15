using SimpleWAWS.Models;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Trace;
using Serilog;

namespace SimpleWAWS.Console
{
    static class Program
    {
        static void Main(string[] args)
        {
            var log = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();
            SimpleTrace.Diagnostics = log;
            SimpleTrace.Analytics = log;
            Task.Run(() => MainAsync()).Wait();
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
            var subscriptionNames = System.Environment.GetEnvironmentVariable("Subscriptions").Split(',');
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
            var subscriptions = await subscriptionsIds.Select(s => new Subscription(s).Load()).WhenAll();
            console("done loading subscriptions");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Where(r => !r.Tags.ContainsKey("CommonApiAppsDeployed")).Count()) + " bad resourceGroups");

            console("calling MakeTrialSubscription on all subscriptions");
            await subscriptions.Select(async s =>
            {

                var result = s.MakeTrialSubscription();
                foreach (var resourceGroup in result.Ready)
                {
                    try
                    {
                        await resourceGroup.PutInDesiredState();
                    }
                    catch (Exception ex)
                    {
                        console(
                            $"RG PIDS Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
                    }
                }
                foreach (var geoRegion in result.ToCreateInRegions)
                {
                    try
                    {
                        await CsmManager.CreateResourceGroup(s.SubscriptionId, geoRegion).Result.PutInDesiredState();
                    }
                    catch (Exception ex)
                    {
                        console($"GR Create Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
                    }
                }
                foreach (var resourceGroup in result.ToDelete)
                {
                    try { await resourceGroup.Delete(true); }
                    catch (Exception ex)
                    {
                        console($"RG Delete Exception:{ex.ToString()}-{ex.StackTrace}-{ex.InnerException?.StackTrace.ToString() ?? String.Empty}");
                    }
                }
            }).WhenAll();
            console("done calling make trial subscription");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");
            console(subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()));
            console("make free trial");
            //Parallel.ForEach(
            //    subscriptions.SelectMany(subscription => subscription.ResourceGroups), async (resourcegroup) =>
            //    {
            //        console($"Deleting resourcegroup:{resourcegroup.ResourceGroupName} from \tsubscription:{resourcegroup.SubscriptionId}");
            //        await resourcegroup.Delete(true);
            //    });

            console("Re-start loading subscriptions");
            console("We have " + subscriptionsIds.Count() + " subscriptions");
            subscriptions = await subscriptionsIds.Select(s => new Subscription(s).Load()).WhenAll();
            console("done loading subscriptions");

            //Parallel.ForEach(
            //    subscriptions.SelectMany(subscription => subscription.ResourceGroups), async (resourcegroup) =>
            //    {
            ////foreach (var resourcegroup in subscriptions.SelectMany(subscription => subscription.ResourceGroups))
            ////{
            ////    try
            ////    {
            ////        console($" Replacing Resource Group : {resourcegroup.CsmId}");
            ////        await resourcegroup.Delete(true);//DeleteAndCreateReplacement(true);
            ////        console($" Replaced");
            ////    }
            ////    catch (Exception ex)
            ////    {
            ////        console(ex.ToString());
            ////    }
            
            ////}

                //}
                //);
            //subscriptions.ToList().ForEach(printSub);
            console("Done");
        }

     
        public static async Task Main2Async()
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
    }
}
