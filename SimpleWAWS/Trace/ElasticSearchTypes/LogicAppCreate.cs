using Nest;
using System;

namespace SimpleWAWS.Trace.ElasticSearchTypes
{
    [ElasticType]
    public class LogicAppCreate
    {
        [ElasticProperty(Name = "@timestamp")]
        public DateTime Timestamp { get; set; }

        [ElasticProperty(Name = "template_name")]
        public string TemplateName { get; set; }

        [ElasticProperty(Name = "unique_id")]
        public string UniqueId { get; set; }

        [ElasticProperty(Name = "azure_portal")]
        public bool AzurePortal { get; set; }

        [ElasticProperty(Name = "extended")]
        public bool Extended { get; set; }

        [ElasticProperty(Name = "user_deleted")]
        public bool UserDeleted { get; set; }

        [ElasticProperty(Name = "deleted_after_minutes")]
        public int DeletedAfterMinutes { get; set; }
    }
}