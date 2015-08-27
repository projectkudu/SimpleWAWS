using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class VkAuthProvider : BaseAuthProvider
    {
        public override void AuthenticateRequest(HttpContextBase context)
        {
            base.AuthenticateRequest(context, TryAuthenticateVkRequest);
        }

        public override string GetLoginUrl(HttpContextBase context)
        {
            var culture = CultureInfo.CurrentCulture.Name.ToLowerInvariant();
            var builder = new StringBuilder();
            builder.Append("https://oauth.vk.com/authorize");
            builder.AppendFormat("?client_id={0}", AuthSettings.VkClientId);
            builder.Append("&scope=email");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format("https://{0}/", context.Request.Headers["HOST"])));
            builder.Append("&response_type=code");
            builder.Append("&v=5.35");
            // Vk.com UrlDecode the state query before passing it back to us. This is different from how AAD, Google and Facebook do it.
            // Hence the double encoding below to work around that issue.
            builder.AppendFormat("&state={0}", WebUtility.UrlEncode(WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format("/{0}{1}", culture, context.Request.Url.Query) : context.Request.Url.PathAndQuery)));
            return builder.ToString();
        }

        public override bool HasToken(HttpContextBase context)
        {
            return context.Request.QueryString["code"] != null;
        }

        protected TokenResults TryAuthenticateVkRequest(HttpContextBase context)
        {
            var code = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                return TokenResults.DoesntExist;
            }

            var user = GetUserFromGraph(code, context);

            if (user == null)
            {
                return TokenResults.ExistAndWrong;
            }
            context.User = user;
            return TokenResults.ExistsAndCorrect;
        }

        private IPrincipal GetUserFromGraph(string code, HttpContextBase context)
        {
            var vkAccessTokenResponse = AuthUtilities.GetContentFromUrl(GetGraphUrl(code, context));
            var vkAccessToken = JsonConvert.DeserializeObject<VkAccessTokenResponse>(vkAccessTokenResponse);
            if (string.IsNullOrEmpty(vkAccessToken.AccessToken))
            {
                return null;
            }

            return new TryWebsitesPrincipal(new TryWebsitesIdentity(vkAccessToken.Email ?? vkAccessToken.UserId.ToString(), vkAccessToken.UserId.ToString(), "Vk"));
        }

        private string GetGraphUrl(string code, HttpContextBase context)
        {
            var builder = new StringBuilder();
            builder.Append("https://oauth.vk.com/access_token");
            builder.AppendFormat("?client_id={0}", AuthSettings.VkClientId);
            builder.AppendFormat("&client_secret={0}", AuthSettings.VkClientSecret);
            builder.AppendFormat("&code={0}", code);
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format("https://{0}/", context.Request.Headers["HOST"], context.Request.Url.Query)));
            return builder.ToString();
        }

        private class VkAccessTokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("user_id")]
            public int UserId { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }
        }
    }
}