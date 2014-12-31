using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class TwitterAuthProvider : BaseOpenIdConnectAuthProvider
    {
        protected override string GetLoginUrl(HttpContext context)
        {
            var builder = new StringBuilder();
            builder.Append("https://www.facebook.com/dialog/oauth");
            builder.Append("?response_type=token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format("https://{0}/", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", "");
            builder.AppendFormat("&scope={0}", "email");
            builder.AppendFormat("&state={0}", WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format("/{0}", context.Request.Url.Query) : context.Request.Url.PathAndQuery));
            return builder.ToString();
        }

        protected override string GetValidAudiance()
        {
            return "";
        }

        protected override string GetIssuerName()
        {
            return "Twitter";
        }
    }
}