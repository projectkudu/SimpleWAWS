using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class FacebookAuthProvider : BaseAuthProvider
    {
        public override void AuthenticateRequest(HttpContext context)
        {
            base.AuthenticateRequest(context, TryAuthenticateFacebookSignedRequest);
        }

        public override bool HasToken(HttpContext context)
        {
            return (context.Request.QueryString["access_token"] != null &&
                    context.Request.QueryString["signed_request"] != null);
        }

        public override string GetLoginUrl(HttpContext context)
        {
            var builder = new StringBuilder();
            builder.Append("https://www.facebook.com/dialog/oauth");
            builder.Append("?response_type=signed_request%20token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format("https://{0}/Login", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", ConfigurationManager.AppSettings["FacebookAppId"]);
            builder.AppendFormat("&scope={0}", "email");
            builder.AppendFormat("&state={0}", WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format("/{0}", context.Request.Url.Query) : context.Request.Url.PathAndQuery));
            return builder.ToString();
        }

        protected TokenResults TryAuthenticateFacebookSignedRequest(HttpContext context)
        {
            var userId = GetUserId(context);
            var accessToken = GetAccessToken(context);

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(userId))
            {
                return TokenResults.DoesntExist;
            }

            var user = GetUserFromGraphApi(userId, accessToken);

            if (user == null)
            {
                return TokenResults.ExistAndWrong;
            }

            context.User = user;
            return TokenResults.ExistsAndCorrect;
        }

        private string GetUserId(HttpContext context)
        {
            var signedRequest = context.Request.QueryString["signed_request"];
            if (string.IsNullOrEmpty(signedRequest)) return null;
            var encodedJsonString = signedRequest.Substring(signedRequest.IndexOf('.') + 1);
            var jsonString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedJsonString.PadBase64()));
            var facebookSignedRequest = JsonConvert.DeserializeObject<FacebookSignedRequest>(jsonString);
            return facebookSignedRequest.UserId;
        }

        private string GetAccessToken(HttpContext context)
        {
            return context.Request.QueryString["access_token"];
        }

        private IPrincipal GetUserFromGraphApi(string userId, string accessToken)
        {
            var jsonUser = GetContentFromUrl(GetGraphUrl(userId, accessToken));
            var fbUser = JsonConvert.DeserializeObject<FacebookUser>(jsonUser);
            if (!userId.Equals(fbUser.Id))
            {
                return null;
            }

            return new TryWebsitesPrincipal(new TryWebsitesIdentity(fbUser.Email ?? fbUser.Id, fbUser.Id, "Facebook"));
        }

        private string GetGraphUrl(string userId, string accessToken)
        {
            var builder = new StringBuilder();
            builder.Append("https://graph.facebook.com/v2.2");
            builder.AppendFormat("/{0}", userId);
            builder.AppendFormat("?access_token={0}", accessToken);
            builder.Append("&format=json");

            return builder.ToString();
        }

        private static string GetContentFromUrl(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            using (var response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private class FacebookSignedRequest
        {
            [JsonProperty("algorithm")]
            public string Algorithm { get; set; }
            [JsonProperty("code")]
            public string Code { get; set; }
            [JsonProperty("issued_at")]
            public int IssuedAt { get; set; }
            [JsonProperty("user_id")]
            public string UserId { get; set; }
        }

        private class FacebookUser
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            [JsonProperty("email")]
            public string Email { get; set; }
            [JsonProperty("first_name")]
            public string FirstName { get; set; }
            [JsonProperty("gender")]
            public string Gender { get; set; }
            [JsonProperty("last_name")]
            public string LastName { get; set; }
            [JsonProperty("link")]
            public string Link { get; set; }
            [JsonProperty("locale")]
            public string Locale { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("timezone")]
            public int Timezone { get; set; }
            [JsonProperty("updated_time")]
            public string UpdatedTime { get; set; }
            [JsonProperty("verified")]
            public bool Verified { get; set; }

        }
    }
}