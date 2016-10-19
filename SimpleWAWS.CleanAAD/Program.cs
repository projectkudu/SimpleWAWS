
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
            switch (args.Length)
            {
                case 0:
                    Run().Wait();
                    break;
                case 2:
                    if (args[0] == "DeleteUser")
                    {
                        DeleteUser(args[1]).Wait();
                    }
                    break;
                default:
                    console("Usage : SimpleWAWS.CleanAAD.exe or SimpleWAWS.CleanAAD.exe DeleteUser <lowercaseusername> ");
                    break;
            }
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
                var usersToDelete = users.value.Where(user => user.acceptedOn != null && user.acceptedOn < DateTime.UtcNow.AddDays(-2) && IsNotAdminUserName(user.displayName)).ToList();
                if (!usersToDelete.Any())
                {
                    if (tasks.Any())
                    {
                        await Task.WhenAll(tasks);
                    }
                    return;
                }
                else
                {
                    foreach (var user in usersToDelete)
                    {
                        Console.WriteLine(user.displayName);
                        tasks.Add(GraphClient.Users[user.objectId].DeleteAsync());

                        if (tasks.Count >= 50)
                        {
                            await Task.WhenAll(tasks);
                            tasks.Clear();
                        }
                    }
                }
            }
        }

        private static bool IsNotAdminUserName(string usernName)
        {
            return
                new[] { "ahmels", "graphAdmin", "trywebsitesnow", "faiz_a_shaikh", "arroyc", "soninaren", "rcarun", "odvoskin", "yochay", "aaronl", "modembug", "cory.fowler" }.All(
                    n => usernName.IndexOf(n, StringComparison.OrdinalIgnoreCase) == -1);
        }

        private static async Task DeleteUser(string username)
        {
            if (IsNotAdminUserName(username))
            {
                //await RemoveTryAppResource(username);
                dynamic GraphClient;
                GraphClient =
                    await
                        ARMLib.GetDynamicClient(apiVersion: "1.42-previewInternal",
                            url: string.Format("{0}/{1}", "https://graph.windows.net", SimpleSettings.TryTenantId))
                            .ConfigureLogin(LoginType.Upn, SimpleSettings.TryUserName, SimpleSettings.TryPassword);
                    var users = (GraphArray) await GraphClient.Users.Query("$top=999").GetAsync<GraphArray>();
                    foreach (var user in users.value)
                    {
                        if (user.acceptedOn != null &&
                            new[] {username}.All(
                                n => user.displayName.IndexOf(n, StringComparison.OrdinalIgnoreCase) > -1))
                        {
                            console(
                                $"Found and deleting:{user.displayName} from tenant:{SimpleSettings.TryTenantId}");
                            await GraphClient.Users[user.objectId].DeleteAsync();
                            return;
                        }
                    }
                }
            else
            {
                console($"Admin Username:{username} . Cannot be deleted");
            }
        }

        private static async Task RemoveTryAppResource(string userName)
        {

            console("start removing tryappservice resourcegroup");
            var manager = await ResourcesManager.GetInstanceAsync();
            manager.DeleteResourceGroup("MSA#"+ userName);
            console("end removing tryappservice resourcegroup");
        }

        private static Action<object> console = (s) => System.Console.WriteLine("[" + (DateTime.Now.ToShortTimeString()) + "] " + s);

    }
}
