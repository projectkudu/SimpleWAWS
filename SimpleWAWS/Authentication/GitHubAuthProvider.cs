using System.Collections.Generic;
using Newtonsoft.Json;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;
using System;

namespace SimpleWAWS.Authentication
{
    public class GitHubAuthProvider : BaseAuthProvider
    {
        public override void AuthenticateRequest(HttpContextBase context)
        {
            base.AuthenticateRequest(context, TryAuthenticateGitHubRequest);
        }

        public override string GetLoginUrl(HttpContextBase context)
        {
            var builder = new StringBuilder();
            builder.Append("https://github.com/login/oauth/authorize");
            builder.AppendFormat("?client_id={0}", AuthSettings.GitHubClientId);
            builder.Append("&scope=user:email");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "https://{0}/", context.Request.Headers["HOST"])));
            builder.Append(LoginStateUrlFragment(context));
            return builder.ToString();
        }

        public override bool HasToken(HttpContextBase context)
        {
            return context.Request.QueryString["code"] != null;
        }

        protected TokenResults TryAuthenticateGitHubRequest(HttpContextBase context)
        {
            var code = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                return TokenResults.DoesntExist;
            }

            var user = GetUserFromGraph(code);

            if (user == null)
            {
                return TokenResults.ExistAndWrong;
            }
            context.User = user;
            return TokenResults.ExistsAndCorrect;
        }

        private IPrincipal GetUserFromGraph(string accessToken)
        {
            //treat the provided github code as the access_token
            if (string.IsNullOrEmpty(accessToken))
            {
                return null;
            }
            //Now get user's emailid
            var githubUserEmailsResponse = AuthUtilities.GetContentFromGitHubUrl(GetGitHubUserUrl(), addGitHubHeaders: true, AuthorizationHeader: GetGitHubAuthHeader(accessToken));
            var githubUserEmails = JsonConvert.DeserializeObject<IList<GitHubUserEmailResponse>>(githubUserEmailsResponse);
            var primaryEmail = githubUserEmails.FirstOrDefault(em => em.Primary && em.Verified);
            if (primaryEmail == null)
            {
                return null;
            }
            return new TryWebsitesPrincipal(new TryWebsitesIdentity(primaryEmail.Email, primaryEmail.Email, "GitHub"));
        }

        private string GetGitHubAuthHeader(string token)
        {
            return "token " + token;
        }

        private string GetGitHubUserUrl()
        {
            return "https://api.github.com/user/emails";
        }


        private class GitHubUserEmailResponse
        {
            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("verified")]
            public bool Verified { get; set; }

            [JsonProperty("primary")]
            public bool Primary { get; set; }
        }

    }
}