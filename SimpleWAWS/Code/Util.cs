using System;
using System.Threading.Tasks;
using SimpleWAWS.Trace;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using Kudu.Client.Zip;
using Kudu.Client.Editor;
using SimpleWAWS.Code.CsmExtensions;
using SimpleWAWS.Code;
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SimpleWAWS.Models
{
    class Util
    {
        public static async Task SafeGuard(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "SafeGuard Exception");
            }
        }
        public static async Task<T> SafeGuard<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "SafeGuard<T> Exception");
                return default(T);
            }
        }

        //http://stackoverflow.com/a/1054087
        static Random random = new Random();
        public static string GetRandomHexNumber(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result.ToLowerInvariant();
            return result + random.Next(16).ToString("X").ToLowerInvariant();
        }
        public static void FireAndForget(string hostName)
        {
            try
            {
                var httpHeaders = "GET / HTTP/1.0\r\n" +
                "Host: " + hostName + "\r\n" +
                "\r\n";
                using (var tcpClient = new TcpClient(hostName, 80))
                {
                    tcpClient.Client.Send(Encoding.ASCII.GetBytes(httpHeaders));
                    tcpClient.Close();
                }
            }
            catch (Exception ex)
            {
                //log and ignore any tcp exceptions
                SimpleTrace.Diagnostics.Error(ex, "TCP Error");
            }
        }

        public static void WarmUpSite(Site site) {
            FireAndForget(site.HostName);
            FireAndForget(site.ScmHostName);
        }
        public static async Task DeployLinuxTemplateToSite(BaseTemplate template, Site site)
        {
            if (template?.MSDeployPackageUrl != null)
            {
                try
                {
                    var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                    var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials, retryCount: 3);
                    Task zipUpload = zipManager.PutZipFileAsync("site/wwwroot", template.MSDeployPackageUrl);
                    var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);
                    Task deleteHostingStart = vfsManager.Delete("site/wwwroot/hostingstart.html");

                    await Task.WhenAll(zipUpload);
                    if (template.Name.Equals(Constants.PHPWebAppLinuxTemplateName, StringComparison.OrdinalIgnoreCase))
                    {
                        await site.UpdateConfig(
                            new {
                                    properties = new {
                                        linuxFxVersion = "PHP|7.2",
                                        appCommandLine = "process.json",
                                        alwaysOn = true
                                    }
                            });
                    }
                    else
                    {
                        await site.UpdateConfig(
                            new {
                                properties = new {
                                    linuxFxVersion = "NODE|9.4",
                                    appCommandLine = "process.json",
                                    alwaysOn = true
                                }
                            });
                    }
                    await Task.Delay(5 * 1000);
                    var lsm = new LinuxSiteManager.Client.LinuxSiteManager(retryCount: 2);
                    Task checkSite = lsm.CheckSiteDeploymentStatusAsync(site.Url);
                    try
                    {
                        await checkSite;
                    }
                    catch (Exception ex)
                    {
                        SimpleTrace.TraceError("New Site wasnt deployed" + ex.Message + ex.StackTrace);
                    }
                }
                catch (Exception ex)
                {
                    SimpleTrace.TraceError(ex.Message + ex.StackTrace);
                }

                WarmUpSite(site);
            }
        }
        public static async Task AddTimeStampFile(Site site)
        {
            var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
            var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);
            var json = JsonConvert.SerializeObject(new { expiryTime = DateTime.UtcNow.AddMinutes(Double.Parse(SimpleSettings.VSCodeLinuxExpiryMinutes)).ToString() });
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            Task addTimeStampFile = vfsManager.Put("site/wwwroot/metadata.json" , content);
            await addTimeStampFile;
        }
        //TODO: Remove this when the setting can be persisted on the first try during ARM create
        public static async Task UpdateVSCodeLinuxConfig(Site site)
        {
            site.AppSettings["SITE_GIT_URL"] = site.GitUrlWithCreds;
            site.AppSettings["SITE_BASH_GIT_URL"] = site.BashGitUrlWithCreds;
            await Task.WhenAll(site.UpdateConfig(
                new
                {
                    properties = new
                    {
                        appCommandLine = "process.json",
                        linuxFxVersion = "NODE|9.4",
                        alwaysOn = true
                    }
                }), site.UpdateAppSettings());
        }
        public static async Task DeployVSCodeLinuxTemplateToSite(BaseTemplate template, Site site, bool addTimeStampFile = false)
        {
            if (template?.MSDeployPackageUrl != null)
            {
                try
                {
                    var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                    var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials, retryCount: 3);
                    Task zipUpload = zipManager.PutZipFileAsync("site/wwwroot", template.MSDeployPackageUrl);
                    var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);
                    Task deleteHostingStart = vfsManager.Delete("site/wwwroot/hostingstart.html");
                    List<Task> taskList = new List<Task>();
                    taskList.Add(zipUpload);
                    taskList.Add(deleteHostingStart);
                    if (addTimeStampFile)
                    {
                        Task timeStampAdd = AddTimeStampFile(site);
                        taskList.Add(timeStampAdd);
                    }
                    await Task.WhenAll(taskList.ToArray());

                    await Task.Delay(10 * 1000);

                    var lsm = new LinuxSiteManager.Client.LinuxSiteManager(retryCount: 4);
                    Task checkSite = lsm.CheckSiteDeploymentStatusAsync(site.Url);
                    try
                    {
                        await checkSite;
                    }
                    catch (Exception ex)
                    {
                        //TODO: Alert on this specifically
                        SimpleTrace.TraceError("New Site wasnt deployed" + ex.Message + ex.StackTrace);
                    }
                }
                catch (Exception ex)
                {
                    SimpleTrace.TraceError(ex.Message + ex.StackTrace);
                }
                WarmUpSite(site);
            }
        }
    }
}
