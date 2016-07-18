using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class FacebookAuthProvider : BaseAuthProvider
    {
        public override void AuthenticateRequest(HttpContextBase context)
        {
            base.AuthenticateRequest(context, TryAuthenticateFacebookSignedRequest);
        }

        public override bool HasToken(HttpContextBase context)
        {
            return (context.Request.QueryString["access_token"] != null &&
                    context.Request.QueryString["signed_request"] != null);
        }

        public override string GetLoginUrl(HttpContextBase context)
        {
            var culture = CultureInfo.CurrentCulture.Name.ToLowerInvariant();
            var builder = new StringBuilder();
            builder.Append("https://www.facebook.com/dialog/oauth");
            builder.Append("?response_type=signed_request%20token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "https://{0}/Login", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", AuthSettings.FacebookAppId);
            builder.AppendFormat("&scope={0}", "email");
            if (context.IsFunctionsPortalRequest())
            {
                builder.AppendFormat("&state={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}{1}", context.Request.Headers["Referer"], context.Request.Url.Query)));
            }
            else
                builder.AppendFormat("&state={0}", WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format(CultureInfo.InvariantCulture, "{0}{1}", culture, context.Request.Url.Query) : context.Request.Url.PathAndQuery));
            return builder.ToString();
        }

        protected TokenResults TryAuthenticateFacebookSignedRequest(HttpContextBase context)
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

        private string GetUserId(HttpContextBase context)
        {
            var signedRequest = context.Request.QueryString["signed_request"];
            if (string.IsNullOrEmpty(signedRequest)) return null;
            var encodedJsonString = signedRequest.Substring(signedRequest.IndexOf('.') + 1);
            var jsonString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedJsonString.PadBase64()));
            var facebookSignedRequest = JsonConvert.DeserializeObject<FacebookSignedRequest>(jsonString);
            return facebookSignedRequest.UserId;
        }

        private string GetAccessToken(HttpContextBase context)
        {
            return context.Request.QueryString["access_token"];
        }

        private IPrincipal GetUserFromGraphApi(string userId, string accessToken)
        {
            var jsonUser = AuthUtilities.GetContentFromUrl(GetGraphUrl(userId, accessToken));
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

        private class FacebookSignedRequest
        {
            [JsonProperty("algorithm")]
            public string Algorithm { get; set; }
            [JsonProperty("code")]
            public string Code { get; set; }
            [JsonProperty("issued_at")]
            public double IssuedAt { get; set; }
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
            //India Standard Time zone is 5.5 This should be a double not an int.
            [JsonProperty("timezone")]
            public double Timezone { get; set; }
            [JsonProperty("updated_time")]
            public string UpdatedTime { get; set; }
            [JsonProperty("verified")]
            public bool Verified { get; set; }

        }
    }
}