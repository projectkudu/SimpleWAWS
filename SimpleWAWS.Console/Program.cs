using SimpleWAWS.Models;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleWAWS.Code.CsmExtensions;

namespace SimpleWAWS.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(() => MainAsync()).Wait();
        }

        public static async Task MainAsync()
        {
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


            console("start loading subscription c5d49f05-f39f-4ec8-9f9f-28c538605225");
            var subscription = await new Subscription("c5d49f05-f39f-4ec8-9f9f-28c538605225").Load();
            console("done loading subscription " + subscription.SubscriptionId );

            console("subscription has: " + subscription.ResourceGroups.Count() + " resourceGroups");

            console("calling MakeTrialSubscription");
            subscription = await subscription.MakeTrialSubscription();
            console("done calling make trial subscription");

            console(subscription.ResourceGroups.Count());

            //await Task.WhenAll(subscription.ResourceGroups.Select(rg => rg.Delete(false)));

            printSub(subscription);
            console("Done");
        }
    }
}
