using Nest;
using System;

namespace SimpleWAWS.Trace.ElasticSearchTypes
{
    [ElasticType]
    public class WebAppCreate
    {
        [ElasticProperty(Name = "@timestamp")]
        public DateTime Timestamp { get; set; }

        [ElasticProperty(Name = "template_name")]
        public string TemplateName { get; set; }

        [ElasticProperty(Name = "template_language")]
        public string TemplateLanguage { get; set; }

        [ElasticProperty(Name = "unique_id")]
        public string UniqueId { get; set; }

        [ElasticProperty(Name = "activity_minutes")]
        public int ActivityMinutes { get; set; }

        [ElasticProperty(Name = "azure_portal")]
        public bool AzurePortal { get; set; }

        [ElasticProperty(Name = "content_download")]
        public bool ContentDownload { get; set; }

        [ElasticProperty(Name = "publishing_profile")]
        public bool PublishingProfile { get; set; }

        [ElasticProperty(Name = "git_link")]
        public bool GitLink { get; set; }

        [ElasticProperty(Name = "extended")]
        public bool Extended { get; set; }

        [ElasticProperty(Name = "user_deleted")]
        public bool UserDeleted { get; set; }

        [ElasticProperty(Name = "deleted_after_minutes")]
        public int DeletedAfterMinutes { get; set; }
    }
}