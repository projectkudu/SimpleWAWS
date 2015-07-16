using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace TryAppService.Data.Models
{
    public class SiteUsageTime
    {
        [Key]
        [JsonProperty(PropertyName = "uniqueId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid UniqueId { get; set; }

        [JsonProperty(PropertyName = "siteUsageTicks", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long SiteUsageTicks { get; set; }
        
        [NotMapped]
        [JsonProperty(PropertyName = "siteUsageTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TimeSpan UsageTime
        {
            get { return TimeSpan.FromTicks(SiteUsageTicks); }
            set { SiteUsageTicks = value.Ticks; }
        }
    }
}