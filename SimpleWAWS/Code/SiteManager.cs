using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Kudu.Client.Editor;
using Kudu.Client.Zip;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.WebSites;

namespace SimpleWAWS.Code
{
    public class SiteManager
    {
        private readonly SiteNameGenerator _nameGenerator = new SiteNameGenerator();
        public static TimeSpan SiteExpiryTime;
        private X509Certificate2 _cert;

        private readonly Queue<Site> _freeSites = new Queue<Site>();
        private readonly Dictionary<string, Site> _sitesInUse = new Dictionary<string, Site>();

        private Timer _timer;
        private readonly JobHost _jobHost = new JobHost();

        private static SiteManager _instance;
        public static async Task<SiteManager> GetInstanceAsync()
        {
            // TODO: what's the right way of locking when using async?
            if (_instance == null)
            {
                _instance = new SiteManager();
                await _instance.LoadSiteListFromAzureAsync();
            }

            return _instance;
        }

        public SiteManager()
        {
            string pfxPath = ConfigurationManager.AppSettings["pfxPath"];
            if (String.IsNullOrEmpty(pfxPath))
            {
                pfxPath = @"App_Data\cert.pfx";
            }

            pfxPath = Path.Combine(HttpRuntime.AppDomainAppPath, pfxPath);
            _cert = new X509Certificate2(
                pfxPath,
                ConfigurationManager.AppSettings["pfxPassword"],
                X509KeyStorageFlags.MachineKeySet);

            WebSpaceName = ConfigurationManager.AppSettings["webspace"];
            SiteExpiryTime = TimeSpan.FromMinutes(Int32.Parse(ConfigurationManager.AppSettings["siteExpiryMinutes"]));
        }

        public string WebSpaceName { get; set; }

        public async Task LoadSiteListFromAzureAsync()
        {
            List<WebSpace> webSpaces = GetAllWebSpaces().ToList();

            // Ask all webspaces to load their site lists (in parallel)
            await Task.WhenAll(webSpaces.Select(ws => ws.LoadAndCreateSitesAsync()));

            // Get a list of all the sites across all subscriptions/webspaces
            List<Site> allSites = webSpaces.SelectMany(ws => ws.Sites).ToList();

            // Check if the sites are in use and place them in the right list
            foreach (Site site in allSites)
            {
                if (site.IsInUse)
                {
                    Trace.TraceInformation("Loading site {0} into the InUse list", site.Name);
                    _sitesInUse[site.Id] = site;
                }
                else
                {
                    Trace.TraceInformation("Loading site {0} into the Free list", site.Name);
                    _freeSites.Enqueue(site);
                }
            }

            // Do maintenance on the site lists every minute (and start one right now)
            _timer = new Timer(OnTimerElapsed);
            _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(60 * 1000));
        }

        IEnumerable<WebSpace> GetAllWebSpaces()
        {
            string[] subscriptions = ConfigurationManager.AppSettings["subscriptions"].Split(',');
            string[] webspaces = ConfigurationManager.AppSettings["webspaces"].Split(',');

            foreach (string subscription in subscriptions)
            {
                var creds = new CertificateCloudCredentials(subscription, _cert);
                var client = new WebSiteManagementClient(creds);

                foreach (string webSpace in webspaces)
                {
                    yield return new WebSpace(this, client, webSpace, _nameGenerator);
                }
            }
        }

        public async Task MaintainSiteLists()
        {
            await DeleteExpiredSitesAsync();
        }

        private void OnTimerElapsed(object state)
        {
            _jobHost.DoWork(() => { MaintainSiteLists().Wait(); });
        }

        public void OnSiteCreated(Site site)
        {
            _freeSites.Enqueue(site);
        }

        public void OnSiteDeleted(Site site)
        {
            _sitesInUse.Remove(site.Id);
        }

        public async Task DeleteExpiredSitesAsync()
        {
            var siteIdsToDelete = new List<Site>();

            // Find all the expired sites
            foreach (var entry in _sitesInUse)
            {
                if (DateTime.UtcNow - entry.Value.StartTime > SiteExpiryTime)
                {
                    siteIdsToDelete.Add(entry.Value);
                }
            }

            // Delete them
            foreach (var site in siteIdsToDelete)
            {
                Trace.TraceInformation("Deleting expired site {0}", site.Name);
                await site.DeleteAndCreateReplacementAsync();
            }
        }

        public async Task<Site> ActivateSiteAsync(string templateZip)
        {
            Site site = _freeSites.Dequeue();

            Trace.TraceInformation("Site {0} is now in use", site.Name);

            Task markAsInUseTask = site.MarkAsInUseAsync();
            if (templateZip != null)
            {
                var credentials = new NetworkCredential(site.PublishingUserName, site.PublishingPassword);
                var zipManager = new RemoteZipManager(site.ScmUrl + "zip/", credentials);
                Task zipUpload = zipManager.PutZipFileAsync("site/wwwroot", templateZip);
                var vfsManager = new RemoteVfsManager(site.ScmUrl + "vfs/", credentials);
                Task deleteHostingStart = vfsManager.Delete("site/wwwroot/hostingstart.html");


                // Wait at most one second for tasks to complete, then let them finish on their own
                // TODO: how to deal with errors. Ongoing tasks should be tracked by the Site object.
                await Task.WhenAny(
                    Task.WhenAll(markAsInUseTask, zipUpload, deleteHostingStart),
                    Task.Delay(1000));
            }
            _sitesInUse[site.Id] = site;

            return site;
        }

        public Site GetSite(string id)
        {
            Site site;
            _sitesInUse.TryGetValue(id, out site);
            return site;
        }

        public async Task ResetAllFreeSites()
        {
            var list = _freeSites.ToList();
            var taskList = list.Select(site => site.MarkAsInUseAsync()).ToList();
            await Task.WhenAll(taskList);
            _freeSites.Clear();
            list.ForEach(l => _sitesInUse[l.Id] = l);
            taskList.Clear();
            taskList.AddRange(list.Select(site => DeleteSite(site.Id)));
            await Task.WhenAll(taskList);
        }

        public async Task DeleteSite(string id)
        {
            Site site;
            _sitesInUse.TryGetValue(id, out site);

            if (site != null)
            {
                Trace.TraceInformation("Deleting site {0}", site.Name);
                await site.DeleteAndCreateReplacementAsync();
            }
        }
    }

}
