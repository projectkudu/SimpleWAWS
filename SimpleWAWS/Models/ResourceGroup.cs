﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace SimpleWAWS.Models
{
    public class ResourceGroup : BaseResource
    {
        private const string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}";

        public override string CsmId
        {
            get
            {
                return string.Format(_csmIdTemplate, SubscriptionId, ResourceGroupName);
            }
        }

        public string UserId
        {
            get { return Tags.ContainsKey(Constants.UserId) ? Tags[Constants.UserId] : null; }
        }

        public DateTime StartTime 
        {
            get { return DateTime.Parse(Tags[Constants.StartTime]); }
        }

        private readonly TimeSpan UsageTimeSpan = TimeSpan.FromMinutes(int.Parse(ConfigurationManager.AppSettings["siteExpiryMinutes"]));

        public string TimeLeft
        {
            get
            {
                TimeSpan timeUsed = DateTime.UtcNow - StartTime;
                TimeSpan timeLeft;
                if (timeUsed > UsageTimeSpan)
                {
                    timeLeft = TimeSpan.FromMinutes(0);
                }
                else
                {
                    timeLeft = UsageTimeSpan - timeUsed;
                }

                return String.Format("{0}m:{1:D2}s", timeLeft.Minutes, timeLeft.Seconds);
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

        public IEnumerable<Site> Sites { get; set; }

        public IEnumerable<ApiApp> ApiApps { get; set; }

        public IEnumerable<Gateway> Gateways { get; set; }

        public IEnumerable<ServerFarm> ServerFarms { get; set; }

        public string GeoRegion 
        {
            get { return Tags[Constants.GeoRegion]; }
        }

        public bool IsRbacEnabled
        {
            get { return bool.Parse(Tags[Constants.IsRbacEnabled]); }
            set { Tags[Constants.IsRbacEnabled] = value.ToString(); }
        }

        public Dictionary<string, string> Tags { get; set; }

        public bool IsSimpleWAWS
        {
            get
            {
                return !string.IsNullOrEmpty(ResourceGroupName) && ResourceGroupName.StartsWith("TRY-AZURE-RG-");
            }
        }

        public UIResource UIResource
        {
            get
            {
                string ibizaUrl = null;
                Site siteToUseForUi = null;
                switch (AppService)
                {
                    case Models.AppService.Web:
                        siteToUseForUi = Sites.First();
                        ibizaUrl = siteToUseForUi.IbizaUrl;
                        break;
                    case Models.AppService.Mobile:
                        siteToUseForUi = Sites.First();
                        break;
                    case Models.AppService.Api:
                        siteToUseForUi = Sites.First(s => s.SiteName.StartsWith("TrySample", StringComparison.OrdinalIgnoreCase));
                        ibizaUrl = ApiApps.First().IbizaUrl;
                        break;
                }

                return siteToUseForUi == null 
                ? null
                : new UIResource
                {
                    Url = AppService == Models.AppService.Mobile ? siteToUseForUi.MobileUrl : siteToUseForUi.Url,
                    IbizaUrl = ibizaUrl,
                    MonacoUrl = siteToUseForUi.MonacoUrl,
                    ContentDownloadUrl = siteToUseForUi.ContentDownloadUrl,
                    GitUrl = siteToUseForUi.GitUrlWithCreds,
                    TimeLeftString = TimeLeft,
                    IsRbacEnabled = IsRbacEnabled,
                    AppService = AppService
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
            this.Tags = new Dictionary<string, string>();
        }
    }
}