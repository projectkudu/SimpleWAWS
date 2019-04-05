using Newtonsoft.Json;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Web.Security;

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
        public string SiteGuid { get { return Tags.ContainsKey(Constants.SiteGuid) ? Tags[Constants.SiteGuid] : String.Empty; } }
        public string UserId
        {
            get { return Tags.ContainsKey(Constants.UserId) ? Tags.ContainsKey(Constants.UserId2)? Tags[Constants.UserId] + Tags[Constants.UserId2] : Tags[Constants.UserId] : null; }
        }

        public DateTime StartTime
        {
            get { return Tags.ContainsKey(Constants.StartTime) ?DateTime.Parse(Tags[Constants.StartTime], CultureInfo.InvariantCulture):DateTime.UtcNow; }
        }

        public TimeSpan LifeTime
        {
            get { return TimeSpan.FromMinutes(int.Parse( Tags.ContainsKey(Constants.UserId) ? Tags[Constants.LifeTimeInMinutes] : "0" )); }
        }

        public static readonly TimeSpan DefaultUsageTimeSpan = TimeSpan.FromMinutes(int.Parse(SimpleSettings.SiteExpiryMinutes, CultureInfo.InvariantCulture));
        public static readonly TimeSpan ExtendedUsageTimeSpan = TimeSpan.FromHours(int.Parse(SimpleSettings.ExtendedResourceExpireHours, CultureInfo.InvariantCulture));
        public static readonly TimeSpan LinuxUsageTimeSpan = TimeSpan.FromMinutes(int.Parse(SimpleSettings.LinuxExpiryMinutes, CultureInfo.InvariantCulture));
        public static readonly TimeSpan VSCodeLinuxUsageTimeSpan = TimeSpan.FromMinutes(int.Parse(SimpleSettings.VSCodeLinuxExpiryMinutes, CultureInfo.InvariantCulture));
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
                return (SubscriptionType == SubscriptionType.MonitoringTools) 
                    ? AppService.MonitoringTools
                    : Tags.ContainsKey(Constants.AppService) && Enum.TryParse<AppService>(Tags[Constants.AppService], out appService)
                    ? appService
                    : AppService.Web;
            }
        }

        [JsonIgnore]
        public Site Site { get; set; }



        //[JsonIgnore]
        //public LinuxResource LinuxResources { get; set; }

        [JsonIgnore]
        public IEnumerable<ServerFarm> ServerFarms { get; set; }

        [JsonIgnore]
        public IEnumerable<StorageAccount> StorageAccounts { get; set; }

        public string GeoRegion
        {
            get { return Tags.ContainsKey(Constants.GeoRegion)? Tags[Constants.GeoRegion]: String.Empty; }
        }

        public bool IsRbacEnabled
        {
            get { return false; }
            //get { return SubscriptionType==SubscriptionType.MonitoringTools ? true : bool.Parse(Tags[Constants.IsRbacEnabled]); }
            //set { Tags[Constants.IsRbacEnabled] = value.ToString(); }
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

        public string DeployedTemplateName
        {
            get
            {
                if (Tags.ContainsKey(Constants.TemplateName))
                    return Tags[Constants.TemplateName];
                else return string.Empty;
            }
        }
        public override string TemplateName
        {
            get
            {
                return string.IsNullOrEmpty(DeployedTemplateName)?base.TemplateName:DeployedTemplateName;
            }
        }

        public Dictionary<string, string> Tags { get; set; }

        public bool IsSimpleWAWS
        {
            get
            {
                return IsSimpleWAWSResourceName
                 && Tags != null && !Tags.ContainsKey("Bad")
                && Tags.ContainsKey("FunctionsContainerDeployed");
            }
        }
        public bool IsSimpleWAWSResourceName
        {
            get
            {
                return !string.IsNullOrEmpty(ResourceGroupName) && ResourceGroupName.StartsWith(Constants.TryResourceGroupPrefix, StringComparison.InvariantCulture)
;
            }
        }
        public UIResource UIResource
        {
            get
            {
                string ibizaUrl = null;
                string csmId = null;
                Site siteToUseForUi = null;

                var templateName = Tags.ContainsKey(Constants.TemplateName) ? Tags[Constants.TemplateName] : string.Empty;
                if (string.IsNullOrEmpty(templateName) && AppService == AppService.Logic)
                {
                    templateName = TemplatesManager.GetTemplates().FirstOrDefault((template) => template.AppService == AppService.Logic)?.Name;
                }
                if (string.IsNullOrEmpty(templateName) && AppService == AppService.MonitoringTools)
                {
                    templateName = TemplatesManager.GetTemplates().FirstOrDefault((template) => template.AppService == AppService.MonitoringTools)?.Name;
                }
                return new UIResource
                {
                    SiteName = Site.SiteName,
                    Url = Site.CamelCasedUrl,
                    IbizaUrl = Site.IbizaUrl,
                    MonacoUrl = Site.MonacoUrl,
                    ContentDownloadUrl = Site.ContentDownloadUrl,
                    GitUrl = Site.GitUrlWithCreds,
                    BashGitUrl = Site.BashGitUrlWithCreds,
                    IsRbacEnabled = IsRbacEnabled,
                    AppService = ( AppService == AppService.Linux
                                    ?AppService.Web : AppService) ,
                    TemplateName = templateName,
                    IsExtended = IsExtended,
                    TimeLeftInSeconds = (int)TimeLeft.TotalSeconds,
                    CsmId = csmId,
                    PublishingUserName = Site.PublishingUserName,
                    PublishingPassword = Site.PublishingPassword,
                    SiteGuid = SiteGuid,
                    LoginSession = $"{SiteGuid}-{ResourceUniqueId}"
                };
            }
        }

        public ResourceGroup(string subsciptionId, string resourceGroupName)
            : base(subsciptionId, resourceGroupName)
        {
            this.Site = null;
            this.ServerFarms = Enumerable.Empty<ServerFarm>();
            this.StorageAccounts = Enumerable.Empty<StorageAccount>();
            this.Tags = new Dictionary<string, string>();
        }
        public ResourceGroup(string subsciptionId, string resourceGroupName,string georegion)
    : base(subsciptionId, resourceGroupName)
        {
            this.Site = null;
            this.ServerFarms = Enumerable.Empty<ServerFarm>();
            this.StorageAccounts = Enumerable.Empty<StorageAccount>();
            this.Tags = new Dictionary<string, string>();
            this.Tags[Constants.GeoRegion] = georegion;
            if (this.Tags.ContainsKey(Constants.TemplateName))
            {
                this.TemplateName = this.Tags[Constants.TemplateName];
            }
        }
        public ResourceGroup(string subsciptionId, string resourceGroupName, string georegion,string templateName)
: base(subsciptionId, resourceGroupName, templateName)
        {
            this.Site = null;
            this.ServerFarms = Enumerable.Empty<ServerFarm>();
            this.StorageAccounts = Enumerable.Empty<StorageAccount>();
            this.Tags = new Dictionary<string, string>();
            this.Tags[Constants.GeoRegion] = georegion;
            this.Tags[Constants.TemplateName] = templateName;
            this.TemplateName = templateName;
        }
    }
}