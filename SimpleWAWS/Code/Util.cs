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
using System.IO;

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

        public static void WarmUpSite(Site site)
        {
            SimpleTrace.TraceInformation($"Warming up hostnames: {site.HostName},{site.ScmHostName}->{site.ResourceGroupName}->{site.SubscriptionId}");
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
                            new
                            {
                                properties = new
                                {
                                    linuxFxVersion = "PHP|7.2",
                                    appCommandLine = "process.json",
                                    alwaysOn = true,
                                    httpLoggingEnabled = true
                                }
                            });
                    }
                    else
                    {
                        await site.UpdateConfig(
                            new
                            {
                                properties = new
                                {
                                    linuxFxVersion = "NODE|9.4",
                                    appCommandLine = "process.json",
                                    alwaysOn = true,
                                    httpLoggingEnabled = true
                                }
                            });
                    }
                    await Task.Delay(5 * 1000);
                    var lsm = new LinuxSiteManager.Client.LinuxSiteManager(retryCount: 2);
                    Task checkSite = lsm.CheckSiteDeploymentStatusAsync(site.HttpUrl);
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
            Task addTimeStampFile = vfsManager.Put("site/wwwroot/metadata.json", content);
            await addTimeStampFile;
        }
        public static async Task UpdateVSCodeLinuxAppSettings(Site site)
        {
            SimpleTrace.TraceInformation($"Site AppSettings Update started: for {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");

            site.AppSettings["SITE_GIT_URL"] = site.GitUrlWithCreds;
            site.AppSettings["SITE_BASH_GIT_URL"] = site.BashGitUrlWithCreds;
            await Task.WhenAll(site.UpdateConfig(
                new
                {
                    properties = new
                    {
                        appCommandLine = "process.json",
                        linuxFxVersion = "NODE|9.4",
                        alwaysOn = true,
                        httpLoggingEnabled = true
                    }
                }), site.UpdateAppSettings());
        }
        public static async Task DeployVSCodeLinuxTemplateToSite(BaseTemplate template, Site site, bool addTimeStampFile = false)
        {
            SimpleTrace.TraceInformation($"Site ZipDeploy started: for {template?.MSDeployPackageUrl} on {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");
            if (template?.MSDeployPackageUrl != null)
            {
                try
                {
                    var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                    var zipManager = new RemoteZipManager(site.ScmUrl + "api/zipdeploy?isAsync=true", credentials, retryCount: 3);
                    Task<Uri> zipUpload = zipManager.PostZipFileAsync("", template.MSDeployPackageUrl);
                    var deploystatusurl = await zipUpload;

                    SimpleTrace.TraceInformation($"Site ZipDeployed: StatusUrl: {deploystatusurl} for {template?.MSDeployPackageUrl} on {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");

                    var deploycheck = 0;
                    var deploycheckTimes = 150;
                    while (deploycheck++ < deploycheckTimes)
                    {
                        try
                        {
                            await Task.Delay(10 * 1000);

                            var url = site.MonacoUrl.Replace(@"/basicauth", deploystatusurl.PathAndQuery);
                            var httpClient = (HttpWebRequest)WebRequest.Create(url);
                            {
                                var creds = $"{site.PublishingUserName }:{ site.PublishingPassword}";
                                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(creds);
                                var credsbase64 = System.Convert.ToBase64String(plainTextBytes);
                                httpClient.Headers.Add($"Authorization: Basic {credsbase64}");
                                using (var response = await httpClient.GetResponseAsync())
                                {
                                    using (var content = new StreamReader(response.GetResponseStream()))
                                    {
                                        var message = Newtonsoft.Json.Linq.JObject.Parse(content.ReadToEnd());
                                        if ((bool)message["complete"] == false)
                                        {
                                            SimpleTrace.TraceInformation($"Zip Deployment going on: StatusUrl: {deploystatusurl} -{JsonConvert.SerializeObject(message)} for {template?.MSDeployPackageUrl} on {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");

                                        }
                                        else
                                        {
                                            SimpleTrace.TraceInformation($"Zip Deployment completed: StatusUrl: {deploystatusurl} -{JsonConvert.SerializeObject(message)} for {template?.MSDeployPackageUrl} on {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");
                                            break;
                                        }
                                    }
                                }

                            }
                        }
                        catch {
                            SimpleTrace.TraceError($"Ping post ZipDeployed: StatusUrl: {deploystatusurl} for {template?.MSDeployPackageUrl} on {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");

                        }

                    }

                    var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials, retryCount: 3);

                    Task deleteHostingStart = vfsManager.Delete("site/wwwroot/hostingstart.html");

                    List<Task> taskList = new List<Task>();
                    //taskList.Add(zipUpload);
                    taskList.Add(deleteHostingStart);
                    if (template.Name == Constants.ReactVSCodeWebAppLinuxTemplateName)
                    {
                        Task uploaddeploymentfile = vfsManager.Put("site/wwwroot/deploy.sh", (template?.MSDeployPackageUrl.Replace("ReactVSCodeWebApp.zip", "deploy.sh")));
                        Task uploaddeploysh = vfsManager.Put("site/wwwroot/.deployment", (template?.MSDeployPackageUrl.Replace("ReactVSCodeWebApp.zip", ".deployment")));
                        taskList.Add(uploaddeploymentfile);
                        taskList.Add(uploaddeploysh);
                    }
                    if (addTimeStampFile)
                    {
                        SimpleTrace.TraceInformation($"Adding TimeStamp File started: for {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");
                        Task timeStampAdd = AddTimeStampFile(site);
                        taskList.Add(timeStampAdd);
                    }
                    await Task.WhenAll(taskList.ToArray());
                    SimpleTrace.TraceInformation($"Site ZipDeploy and Delete HostingStart completed: for {site.SiteName}->{site.ResourceGroupName}->{site.SubscriptionId}");
                    await Task.Delay(10 * 1000);
                }
                catch (Exception ex)
                {
                    var message = "New Site wasnt deployed: " + ex.Message + ex.StackTrace;
                    SimpleTrace.TraceError(message);
                    throw new ZipDeploymentFailedException(message);
                }
            }
            else
            {
                SimpleTrace.TraceError("New Site wasnt deployed: MsDeployPackageUrl wasnt set");
            }
        }
        internal static async Task<bool> PingTillStatusCode(string path, HttpStatusCode statusCode, int tries, int retryInterval)
        {
                var trial = 0;
                while (trial++ < tries)
                {
                    await Task.Delay(new TimeSpan(0, 0, 0, retryInterval, 0));
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            using (var request = new HttpRequestMessage())
                            {
                                request.Method = HttpMethod.Get;
                                request.RequestUri = new Uri($"http://{path}", UriKind.Absolute);
                                var response = await httpClient.SendAsync(request);
                                if (response.StatusCode == statusCode)
                                {
                                    await response.Content.ReadAsStringAsync();
                                    SimpleTrace.TraceInformation($"Found StatusCode {statusCode.ToString()} at {path} after {trial} trial of {tries}");
                                    return true;
                                }
                            }
                        }
                    } catch (Exception ex)
                    {
                        SimpleTrace.TraceError($"Error pinging {path} on {trial} of {tries}-> {ex.Message} -> {ex.StackTrace}");
                    }
                }
            SimpleTrace.TraceInformation($"Didnt get StatusCode {statusCode.ToString()} at {path} after {tries} ");
            return false;
        }
    }
}
