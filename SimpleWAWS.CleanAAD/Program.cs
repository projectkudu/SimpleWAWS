using ARMClient.Library;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWAWS.CleanAAD
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
        }

        static async Task Run()
        {
            dynamic GraphClient;
            GraphClient = await ARMLib.GetDynamicClient(apiVersion: "1.42-previewInternal", url: string.Format("{0}/{1}", "https://graph.windows.net", SimpleSettings.TryTenantId))
                                .ConfigureLogin(LoginType.Upn, SimpleSettings.TryUserName, SimpleSettings.TryPassword);
            var tasks = new List<Task>();
            while (true)
            {
                var users = (GraphArray)await GraphClient.Users.Query("$top=999").GetAsync<GraphArray>();
                foreach (var user in users.value)
                {
                    if (user.acceptedOn != null &&
                        user.acceptedOn < DateTime.UtcNow.AddDays(-2) &&
                        new[] { "ahmels", "graphAdmin", "trywebsitesnow" }.All(n => user.displayName.IndexOf(n, StringComparison.OrdinalIgnoreCase) == -1))
                    {
                        Console.WriteLine(user.displayName);
                        tasks.Add(GraphClient.Users[user.objectId].DeleteAsync());
                    }

                    if (tasks.Count >= 50)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }
                }
            }
        }
    }
}
