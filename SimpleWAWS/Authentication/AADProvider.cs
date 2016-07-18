using System;
using System.Configuration;
using System.Net;
using System.Text;
using System.Web;
using SimpleWAWS.Models;
using System.Globalization;

namespace SimpleWAWS.Authentication
{
    public class AADProvider : BaseOpenIdConnectAuthProvider
    {
        public override string GetLoginUrl(HttpContextBase context)
        {
            var culture = CultureInfo.CurrentCulture.Name.ToLowerInvariant();
            var builder = new StringBuilder();
            builder.Append(AuthSettings.BaseLoginUrl);
            builder.Append("?response_type=id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "https://{0}/", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", AuthSettings.AADAppId);
            builder.Append("&response_mode=query");
            builder.AppendFormat("&resource={0}", WebUtility.UrlEncode("https://management.core.windows.net/"));
            builder.AppendFormat("&site_id={0}", "500879");
            builder.AppendFormat("&nonce={0}", Guid.NewGuid());
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
            return AuthSettings.AADAppId;
        }

        public override string GetIssuerName(string altSecId)
        {
            return AADProvider.IsMSA(altSecId) ? "MSA" : "OrgId";
        }

        public static bool IsMSA(string altSecId)
        {
            return altSecId != null && altSecId.IndexOf("live.com", StringComparison.OrdinalIgnoreCase) != -1;
        }
    }
}