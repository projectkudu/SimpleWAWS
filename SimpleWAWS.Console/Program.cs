using SimpleWAWS.Models;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
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
            console("We have " + subscriptionNames.Count() + " subscriptions");
            var subscriptions = await subscriptionNames.Select(s => new Subscription(s).Load()).WhenAll();
            console("done loading subscriptions");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");

            console("calling MakeTrialSubscription on all subscriptions");
            subscriptions = await subscriptions.Select(s => s.MakeTrialSubscription()).WhenAll();
            console("done calling make trial subscription");

            console("subscriptions have: " + subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()) + " resourceGroups");
            //console(subscriptions.Aggregate(0, (count, sub) => count += sub.ResourceGroups.Count()));

            //await Task.WhenAll(subscriptions.Select(subscription => subscription.ResourceGroups.Select(rg => rg.Delete(true))).SelectMany(i => i));

            //subscriptions.ToList().ForEach(printSub);
            console("Done");
        }

        public static async Task Main2Async()
        {
            var startTime = DateTime.UtcNow;
            Action<object> console = (s) => System.Console.WriteLine("[" + (DateTime.UtcNow - startTime).TotalMilliseconds + "] " + s);

            console("start");
            var manager = await ResourcesManager.GetInstanceAsync();
            console("done initial loading");

            console("activate api app");
            var resourceGroup = await manager.ActivateApiApp(new ApiTemplate { Name = "TrySamplesContactList" }, new Authentication.TryWebsitesIdentity("test@test.com", null, "AAD"));
            console("done activating api app");

            resourceGroup.PrettyPrint();

        }
    }
}
