using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TryAppService.Data.Models
{
    public class UserLoggedIn
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }
        public string AnonymousUserName { get; set; }
        public string LoggedInUserName { get; set; }
        public DateTime DateTime { get; set; }
    }
}