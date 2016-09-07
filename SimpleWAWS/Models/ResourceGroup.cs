using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using SimpleWAWS.Models.CsmModels;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace SimpleWAWS.Models
{
    public class ResourceGroup : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}";

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, _csmIdTemplate, SubscriptionId, ResourceGroupName);
            }
        }

        public string UserId
        {
            get { return Tags.ContainsKey(Constants.UserId) ? Tags[Constants.UserId] : null; }
        }

        public DateTime StartTime 
        {
            get { return DateTime.Parse(Tags[Constants.StartTime], CultureInfo.InvariantCulture).ToUniversalTime(); }
        }

        public TimeSpan LifeTime
        {
            get{ return TimeSpan.FromMinutes(int.Parse(Tags[Constants.LifeTimeInMinutes])); }
        }

        public static readonly TimeSpan DefaultUsageTimeSpan = TimeSpan.FromMinutes(int.Parse(SimpleSettings.SiteExpiryMinutes, CultureInfo.InvariantCulture));
        public static readonly TimeSpan ExtendedUsageTimeSpan = TimeSpan.FromHours(int.Parse(SimpleSettings.ExtendedResourceExpireHours, CultureInfo.InvariantCulture));
        public static readonly TimeSpan JenkinsUsageTimeSpan = TimeSpan.FromMinutes(int.Parse(SimpleSettings.JenkinsExpiryMinutes, CultureInfo.InvariantCulture));

        public TimeSpan TimeLeft
        {
            get
            {
                TimeSpan timeUsed = DateTime.UtcNow - StartTime;
                TimeSpan timeLeft;

                if (timeUsed > LifeTime)
                {
                    timeLeft = TimeSpan.FromMinutes(0);
                }
                else
                {
                    timeLeft = LifeTime - timeUsed;
                }
                return timeLeft;
            }
        }

        public string ResourceUniqueId 
        {
            get { return ResourceGroupName.Split('_').LastOrDefault(); }
        }

        public AppService AppService 
        {
            get
            {
                var appService = AppService.Web;
                return Tags.ContainsKey(Constants.AppService) && Enum.TryParse<AppService>(Tags[Constants.AppService], out appService)
                    ? appService
                    : AppService.Web;
            }
        }

        [JsonIgnore]
        public IEnumerable<Site> Sites { get; set; }

        [JsonIgnore]
        public IEnumerable<ApiApp> ApiApps { get; set; }

        [JsonIgnore]
        public IEnumerable<LogicApp> LogicApps { get; set; }
        [JsonIgnore]
        public JenkinsResource JenkinsResources { get; set; }

        [JsonIgnore]
        public IEnumerable<Gateway> Gateways { get; set; }

        [JsonIgnore]
        public IEnumerable<ServerFarm> ServerFarms { get; set; }

        [JsonIgnore]
        public IEnumerable<StorageAccount> StorageAccounts { get; set; }

        public string GeoRegion 
        {
            get { return Tags[Constants.GeoRegion]; }
        }

        public bool IsRbacEnabled
        {
            get { return bool.Parse(Tags[Constants.IsRbacEnabled]); }
            set { Tags[Constants.IsRbacEnabled] = value.ToString(); }
        }

        public bool IsExtended
        {
            get
            {
                bool value = false;
                return Tags.ContainsKey(Constants.IsExtended) &&
                       bool.TryParse(Tags[Constants.IsExtended], out value) &&
                       value;
            }
        }

        public Dictionary<string, string> Tags { get; set; }

        public bool IsSimpleWAWS
        {
            get
            {
                return !string.IsNullOrEmpty(ResourceGroupName) && ResourceGroupName.StartsWith("TRY-AZURE-RG-", StringComparison.InvariantCulture);
            }
        }

        public UIResource UIResource
        {
            get
            {
                string ibizaUrl = null;
                string csmId = null;
                Site siteToUseForUi = null;
                switch (AppService)
                {
                    case Models.AppService.Web:
                        siteToUseForUi = Sites.Where(s => s.IsSimpleWAWSOriginalSite).First();
                        ibizaUrl = siteToUseForUi.IbizaUrl;
                        break;
                    case Models.AppService.Mobile:
                        siteToUseForUi = Sites.Where(s => s.IsSimpleWAWSOriginalSite).First();
                        break;
                    case Models.AppService.Api:
                        siteToUseForUi = Sites.First(s => s.SiteName.StartsWith("TrySample", StringComparison.OrdinalIgnoreCase));
                        ibizaUrl = ApiApps.First().IbizaUrl;
                        break;
                    case Models.AppService.Logic:
                        ibizaUrl = LogicApps.First().IbizaUrl;
                        break;
                    case Models.AppService.Function:
                        csmId = Sites.Where(s => s.IsFunctionsContainer).First().CsmId;
                        break;
                    case Models.AppService.Jenkins:
                        ibizaUrl = JenkinsResources?.JenkinsResourceUrl;
                        break;
                }
                var templateName = Tags.ContainsKey(Constants.TemplateName) ? Tags[Constants.TemplateName] : string.Empty;

                return (siteToUseForUi == null|| (AppService ==AppService.Jenkins))
                ? new UIResource
                {
                    IbizaUrl = ibizaUrl,
                    IsRbacEnabled = IsRbacEnabled,
                    AppService = AppService,
                    TemplateName = templateName,
                    IsExtended = IsExtended,
                    TimeLeftInSeconds = (int)TimeLeft.TotalSeconds,
                    CsmId = csmId,
                    Url = JenkinsResources?.JenkinsResourceUrl
                }
                : new UIResource
                {
                    Url = siteToUseForUi.Url,
                    MobileWebClient = AppService == Models.AppService.Mobile ? siteToUseForUi.GetMobileUrl(templateName) : null,
                    IbizaUrl = ibizaUrl,
                    MonacoUrl = siteToUseForUi.MonacoUrl,
                    ContentDownloadUrl = siteToUseForUi.ContentDownloadUrl,
                    GitUrl = siteToUseForUi.GitUrlWithCreds,
                    IsRbacEnabled = IsRbacEnabled,
                    AppService = AppService,
                    TemplateName = templateName,
                    IsExtended = IsExtended,
                    TimeLeftInSeconds = (int)TimeLeft.TotalSeconds,
                    CsmId = csmId
                };
            }
        }

        public UIResource FunctionsUIResource
        {
            get
            {
                var templateName = Tags.ContainsKey(Constants.TemplateName) ? Tags[Constants.TemplateName] : string.Empty;
                var appService = AppService.Function;
                var siteToUseForUi = Sites.First(s => s.IsFunctionsContainer);

                return new UIResource
                {
                    Url = siteToUseForUi.Url,
                    MobileWebClient = null,
                    IbizaUrl = siteToUseForUi.IbizaUrl,
                    MonacoUrl = siteToUseForUi.MonacoUrl,
                    ContentDownloadUrl = siteToUseForUi.ContentDownloadUrl,
                    GitUrl = siteToUseForUi.GitUrlWithCreds,
                    IsRbacEnabled = IsRbacEnabled,
                    AppService = appService,
                    TemplateName = templateName,
                    IsExtended = IsExtended,
                    TimeLeftInSeconds = (int)TimeLeft.TotalSeconds,
                    CsmId = siteToUseForUi.CsmId
                };
            }
        }

        public ResourceGroup(string subsciptionId, string resourceGroupName)
            : base(subsciptionId, resourceGroupName)
        {
            this.Sites = Enumerable.Empty<Site>();
            this.ApiApps = Enumerable.Empty<ApiApp>();
            this.Gateways = Enumerable.Empty<Gateway>();
            this.ServerFarms = Enumerable.Empty<ServerFarm>();
            this.LogicApps = Enumerable.Empty<LogicApp>();
            this.StorageAccounts = Enumerable.Empty<StorageAccount>();
            this.Tags = new Dictionary<string, string>();
        }
    }
}