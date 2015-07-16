using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace TryAppService.Data.Models
{
    public class FreeTrialClick
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "userName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "freeTrialClicks", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int FreeTrialClicks { get; set; }
    }
}