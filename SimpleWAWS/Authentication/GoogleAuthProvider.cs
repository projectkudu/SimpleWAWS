using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class GoogleAuthProvider : BaseOpenIdConnectAuthProvider
    {
        protected override string GetLoginUrl(HttpContext context)
        {
            var builder = new StringBuilder();
            builder.Append("https://accounts.google.com/o/oauth2/auth");
            builder.Append("?response_type=id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format("https://{0}/Login", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", "504310977207-tk3fjp3s6mk6ph8m3gsnkhaan49ejjaa.apps.googleusercontent.com");
            builder.AppendFormat("&scope={0}", "email");
            builder.AppendFormat("&state={0}", WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format("/{0}", context.Request.Url.Query) : context.Request.Url.PathAndQuery));
            return builder.ToString();
        }

        protected override string GetValidAudiance()
        {
            return "504310977207-tk3fjp3s6mk6ph8m3gsnkhaan49ejjaa.apps.googleusercontent.com";
        }

        protected override string GetIssuerName()
        {
            return "Google";
        }
    }
}