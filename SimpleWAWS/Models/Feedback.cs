using Newtonsoft.Json;

namespace SimpleWAWS.Models
{
    public class Feedback
    {
        [JsonProperty(PropertyName = "comment")]
        public string Comment { get; set; }

        [JsonProperty(PropertyName = "contactMe")]
        public bool ContactMe { get; set; }
    }
}