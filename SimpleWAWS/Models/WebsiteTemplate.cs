using Newtonsoft.Json;

namespace SimpleWAWS.Models
{
    public class WebsiteTemplate : BaseTemplate
    {
        [JsonProperty(PropertyName="fileName")]
        public string FileName { get; set; }

        [JsonProperty(PropertyName="language")]
        public string Language { get; set; }

        public static WebsiteTemplate EmptySiteTemplate
        {
            get { return new WebsiteTemplate() { Name = "Empty Site", Language = "Default", SpriteName = "sprite-Large" }; }
        }
    }
}