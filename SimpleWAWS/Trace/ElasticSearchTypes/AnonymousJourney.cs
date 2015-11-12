using Nest;
using System;

namespace SimpleWAWS.Trace.ElasticSearchTypes
{
    public class AnonymousJourney
    {
        [ElasticProperty(Name ="@timestamp")]
        public DateTime Timestamp { get; set; }

        [ElasticProperty(Name = "username")]
        public string Username { get; set; }

        [ElasticProperty(Name = "referrer")]
        public string Referrer { get; set; }

        [ElasticProperty(Name = "referrer_catagorized")]
        public string ReferrerCatagorized { get; set; }

        [ElasticProperty(Name = "source_variation")]
        public string SourceVariation { get; set; }

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
    }
}