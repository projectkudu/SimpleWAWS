using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace TryAppService.Data.Models
{
    public class UserPuid
    {
        [Key]
        [JsonIgnore]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "userName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "puid", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Puid { get; set; }

        public override int GetHashCode()
        {
            return Puid.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var userPuid = obj as UserPuid;
            if (userPuid == null)
            {
                return false;
            }

            return StringComparer.InvariantCultureIgnoreCase
                .Equals(this.Puid, userPuid.Puid);
        }
    }
}