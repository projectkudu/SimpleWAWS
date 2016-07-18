using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class TwitterAuthProvider : BaseOpenIdConnectAuthProvider
    {
        public override string GetLoginUrl(HttpContextBase context)
        {
            var culture = CultureInfo.CurrentCulture.Name.ToLowerInvariant();
            var builder = new StringBuilder();
            builder.Append("https://www.facebook.com/dialog/oauth");
            builder.Append("?response_type=token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "https://{0}/", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", "");
            builder.AppendFormat("&scope={0}", "email");
            if (context.IsFunctionsPortalRequest())
            {
                builder.AppendFormat("&state={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}{1}", context.Request.Headers["Referer"], context.Request.Url.Query)));
            }
            else
                builder.AppendFormat("&state={0}", WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format(CultureInfo.InvariantCulture, "{0}{1}", culture, context.Request.Url.Query) : context.Request.Url.PathAndQuery));
            return builder.ToString();
        }

        protected override string GetValidAudiance()
        {
            return "";
        }

        public override string GetIssuerName(string altSecId)
        {
            return "Twitter";
        }
    }
}