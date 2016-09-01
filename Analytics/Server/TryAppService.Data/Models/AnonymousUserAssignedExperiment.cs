using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TryAppService.Data.Models
{
    public class UserAssignedExperiment
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Experiment { get; set; }
        public string Referer { get; set; }
        public string CampaignId { get; set; }
        public string SourceVariation { get; set; }
        public DateTime DateTime { get; set; }
        public string UserCulture { get; set; }

    }
}