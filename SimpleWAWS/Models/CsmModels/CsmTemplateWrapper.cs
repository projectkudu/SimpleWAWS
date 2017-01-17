using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleWAWS.Models.CsmModels
{
    public class CsmTemplateWrapper
    {
        public CsmTemplateProperties properties { get; set; }
    }

    public class CsmTemplateProperties
    {
        public string mode { get; set; }

        public object parameters { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public JObject template { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CsmTemplateLink templateLink { get; set; }
    }
}