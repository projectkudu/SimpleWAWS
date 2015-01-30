using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;

namespace SimpleWAWS.Code
{
    public class WebSpace
    {
        private SiteManager _manager;
        public IWebSiteManagementClient Client { get; private set; }
        private string _webSpaceName;
        private string _webSpaceRegion;
        private readonly SiteNameGenerator _nameGenerator;
        private readonly AsyncLock _lock = new AsyncLock();
        public List<Site> Sites { get; private set; }
        public string SubscriptionId { get; private set; }
        public string ResourceGroup { get; private set; }

        private readonly int _sitesPerWebSpace;

        public WebSpace(SiteManager manager, IWebSiteManagementClient client, string geoRegion, SiteNameGenerator nameGenerator, string subscriptionId)
        {
            _manager = manager;
            Client = client;
            _webSpaceName = geoRegion.Replace(" ", "").ToLower() + "webspace";
            _webSpaceRegion = geoRegion;
            _nameGenerator = nameGenerator;
            _sitesPerWebSpace = Int32.Parse(ConfigurationManager.AppSettings["sitesPerWebspace"]);

            SubscriptionId = subscriptionId;
            ResourceGroup = "Default-Web-" + geoRegion.Replace(" ", "");
        }

        public async Task LoadAndCreateSitesAsync()
        {
            try
            {
                var webSitesRepsonses = await Client.WebSpaces.ListWebSitesAsync(_webSpaceName, new WebSiteListParameters());
                //TODO: there is a bug that I think happens when the "try it now" site restarts while a site from the free list is being created
                //TODO: that bug will cause us to get an incomplete webSiteResponse object here. 
                //TODO: I think we should retry for a second or 2 and then give up and assume the webspace is empty
                Sites = webSitesRepsonses.Select(webSitesRepsonse => new Site(this, webSitesRepsonse)).ToList();
            }
            catch (Exception ex)
            {
                //assume webspace doesn't exist
                Trace.TraceError(ex.ToString());
                Sites = new List<Site>();
            }

            // Load the configuration for all the sites in parallel
            await Task.WhenAll(Sites.Select(s => s.LoadConfigurationAsync()));

            // Keep only the sites we created
            Sites = Sites.Where(site => site.IsSimpleWAWS).ToList();

            // Make sure we have the right number of sites
            await EnsureCorrectSiteCountAsync();
        }

        // Create additional sites if needed to reach the desired count, or delete some if we have too many
        public async Task EnsureCorrectSiteCountAsync()
        {
            using (await this._lock.LockAsync())
            {
                int neededSites = _sitesPerWebSpace - Sites.Count;
                if (neededSites > 0)
                {
                    Trace.TraceInformation("Creating {0} new sites in {1}", neededSites, this);
                    await Task.WhenAll(Enumerable.Range(0, neededSites).Select(x => CreateNewSiteAsync()));
                }
                else
                {
                    // If we have too many, delete some
                    var sitesToRemove = Sites.Where(s => s.UserId == null).Take(-neededSites).ToList();
                    Sites = Sites.Where(s => !sitesToRemove.Contains(s)).ToList();
                    await Task.WhenAll(sitesToRemove.Select(DeleteAsync));
                }
            }
        }

        private async Task CreateNewSiteAsync()
        {
            // Create a blank new site

            string siteName = await GenerateSiteNameAsync();

            //Trace.TraceInformation("Creating site '{0}' in {1}", siteName, this);

            WebSiteCreateResponse webSiteCreateResponse = await Foo(
                () => Client.WebSites.CreateAsync(_webSpaceName,
                    new WebSiteCreateParameters
                    {
                        Name = siteName,
                        WebSpaceName = _webSpaceName,
                        WebSpace = new WebSiteCreateParameters.WebSpaceDetails
                        {
                            GeoRegion = _webSpaceRegion,
                            Name = _webSpaceName,
                            Plan = "VirtualDedicatedPlan"
                        }
                    }),
                "Creating site '{0}' in {1}", siteName, this);

            //Trace.TraceInformation("Created site '{0}' in {1}", siteName, this);

            var site = new Site(this, webSiteCreateResponse.WebSite);
            await site.InitializeNewSite();
            RegisterSite(site);
        }

        async Task<T> Foo<T>(Func<Task<T>> action, string messageFormat, params object[] args)
        {
            string message = String.Format(messageFormat, args);
            int attempt = 1;
            for (; ; )
            {
                try
                {
                    Trace.TraceInformation("Before {0} (attempt #{1})", message, attempt);
                    T ret = await action();
                    Trace.TraceInformation("Completed {0}", message);
                    return ret;
                }
                catch (CloudException e)
                {
                    Trace.TraceInformation("Failed {0} (attempt #{1}): {2}", message, attempt, e);

                    if (e.Response.StatusCode != HttpStatusCode.Conflict) throw;
                    
                    if (++attempt > 3) throw;
                }
            }
        }

        private void RegisterSite(Site site)
        {
            Sites.Add(site);
            _manager.OnSiteCreated(site);
        }

        private async Task<string> GenerateSiteNameAsync()
        {
            string webSiteName = null;
            bool includeNumber = false;
            for (int i = 0; ; i++)
            {
                webSiteName = _nameGenerator.GenerateName(includeNumber);

                if ((await Client.WebSites.IsHostnameAvailableAsync(webSiteName)).IsAvailable) return webSiteName;

                if (i == 1)
                {
                    // Couldn't get a simple name, so append a number to it
                    includeNumber = true;
                }
                else if (i == 4)
                {
                    // Give up after 5 attempts
                    throw new Exception("No available site name");
                }
            }
        }

        public async Task DeleteAndCreateReplacementAsync(Site site)
        {
            // Delete the site, and create a new one to replace it
            // Make sure that deleting a site doesn't throw any errors. 
            // a site might be in a bad state that might cause deleting to fail
            await Util.SafeGuard(async () => await DeleteAsync(site));
            await Util.SafeGuard(async () => await EnsureCorrectSiteCountAsync());
        }

        public async Task DeleteAsync(Site site)
        {
            Sites.Remove(site);
            await _manager.OnSiteDeletedAsync(site);
            await DeleteAsync(site.Name);
        }

        #region Simple IWebSiteManagementClient wrappers
        public Task<WebSiteGetConfigurationResponse> GetConfigurationAsync(string webSiteName)
        {
            return Client.WebSites.GetConfigurationAsync(_webSpaceName, webSiteName);
        }

        public Task<WebSiteGetPublishProfileResponse> GetPublishingProfile(string webSiteName)
        {
            return Client.WebSites.GetPublishProfileAsync(_webSpaceName, webSiteName);
        }

        public Task<OperationResponse> UpdateConfigurationAsync(string webSiteName, WebSiteUpdateConfigurationParameters parameters)
        {
            return Client.WebSites.UpdateConfigurationAsync(_webSpaceName, webSiteName, parameters);
        }

        private Task<OperationResponse> DeleteAsync(string webSiteName)
        {
            return Client.WebSites.DeleteAsync(_webSpaceName, webSiteName, new WebSiteDeleteParameters());
        }
        #endregion

        public override string ToString()
        {
            return String.Format("{0} / {1}", Client.Credentials.SubscriptionId, _webSpaceName);
        }
    }
}
