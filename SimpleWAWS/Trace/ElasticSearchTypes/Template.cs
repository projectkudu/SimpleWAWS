using Nest;
using System;

namespace SimpleWAWS.Trace.ElasticSearchTypes
{
    [ElasticType]
    public class Template
    {
        [ElasticProperty(Name = "@timestamp")]
        public DateTime Timestamp { get; set; }

        [ElasticProperty(Name = "username")]
        public string Username { get; set; }

        [ElasticProperty(Name = "unique_id")]
        public string UniqueId { get; set; }

        [ElasticProperty(Name = "app_service")]
        public string AppService { get; set; }

        [ElasticProperty(Name = "template_name")]
        public string TemplateName { get; set; }

        [ElasticProperty(Name = "template_language")]
        public string TemplateLanguage { get; set; }

        [ElasticProperty(Name = "experiment")]
        public string Experiment { get; set; }

        [ElasticProperty(Name = "culture")]
        public string Culture { get; set; }

        [ElasticProperty(Name = "extended")]
        public bool Extended { get; set; }

        [ElasticProperty(Name = "activity_minutes")]
        public int ActivityMinutes { get; set; }
    }
}