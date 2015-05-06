using System;
using System.Configuration;
using System.Net;
using System.Text;
using System.Web;
using SimpleWAWS.Models;

namespace SimpleWAWS.Authentication
{
    public class AADProvider : BaseOpenIdConnectAuthProvider
    {
        public override string GetLoginUrl(HttpContext context)
        {
            var builder = new StringBuilder();
            builder.Append(ConfigurationManager.AppSettings[Constants.BaseLoginUrl]);
            builder.Append("?response_type=id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format("https://{0}/", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", ConfigurationManager.AppSettings[Constants.AADAppId]);
            builder.Append("&response_mode=query");
            builder.AppendFormat("&resource={0}", WebUtility.UrlEncode("https://management.core.windows.net/"));
            builder.AppendFormat("&site_id={0}", "500879");
            builder.AppendFormat("&nonce={0}", Guid.NewGuid());
            builder.AppendFormat("&state={0}", WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format("/{0}", context.Request.Url.Query) : context.Request.Url.PathAndQuery));
            return builder.ToString();
        }

        protected override string GetValidAudiance()
        {
            return ConfigurationManager.AppSettings[Constants.AADAppId];
        }

        protected override string GetIssuerName()
        {
            return "AAD";
        }
    }
}