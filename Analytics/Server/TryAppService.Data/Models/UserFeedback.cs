using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TryAppService.Data.Models
{
    public class UserFeedback
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }
        public string UserName { get; set; }
        public string AnonymousUserName { get; set; }
        public string Comment { get; set; }
        public bool ContactMe { get; set; }
        public DateTime DateTime { get; set; }
        public string Experiment { get; set; }
        public string SourceVariation { get; set; }
        public string UserCulture { get; set; }
    }
}
