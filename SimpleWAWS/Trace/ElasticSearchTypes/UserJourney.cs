using Nest;
using System;
using System.Collections.Generic;

namespace SimpleWAWS.Trace.ElasticSearchTypes
{
    [ElasticType]
    public class UserJourney
    {
        [ElasticProperty(Name = "@timestamp")]
        public DateTime Timestamp { get; set; }

        [ElasticProperty(Name = "anonymous_username")]
        public string AnonymousUsername { get; set; }

        [ElasticProperty(Name = "username")]
        public string Username { get; set; }

        [ElasticProperty(Name = "referrer")]
        public string Referrer { get; set; }

        [ElasticProperty(Name = "referrer_catagorized")]
        public string ReferrerCatagorized { get; set; }

        [ElasticProperty(Name = "experiment")]
        public string Experiment { get; set; }

        [ElasticProperty(Name = "culture")]
        public string Culture { get; set; }

        [ElasticProperty(Name = "landed_on")]
        public LandedOn LandedOn { get; set; }

        [ElasticProperty(Name = "free_trial_click_top")]
        public bool FreeTrialClickTop { get; set; }

        [ElasticProperty(Name = "free_trial_click_bottom")]
        public bool FreeTrialClickBottom { get; set; }

        [ElasticProperty(Name = "free_trial_click_expire")]
        public bool FreeTrialClickExpire { get; set; }

        [ElasticProperty(Name = "logged_in")]
        public bool LoggedIn { get; set; }

        [ElasticProperty(Name = "web_app_creates")]
        public IEnumerable<WebAppCreate> WebAppCreates { get; set; }

        [ElasticProperty(Name = "mobile_app_creates")]
        public IEnumerable<MobileAppCreate> MobileAppCreates { get; set; }

        [ElasticProperty(Name = "logic_app_creates")]
        public IEnumerable<LogicAppCreate> LogicAppCreates { get; set; }

        [ElasticProperty(Name = "web_apps_count")]
        public int WebAppsCount { get; set; }

        [ElasticProperty(Name = "mobile_apps_count")]
        public int MobileAppsCount { get; set; }

        [ElasticProperty(Name = "logic_apps_count")]
        public int LogicAppsCount { get; set; }

    }
}