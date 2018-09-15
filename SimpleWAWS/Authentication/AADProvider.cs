using System;
using System.Net;
using System.Text;
using System.Web;
using System.Globalization;

namespace SimpleWAWS.Authentication
{
    public class AADProvider : BaseOpenIdConnectAuthProvider
    {
        public override string GetLoginUrl(HttpContextBase context)
        {
            var builder = new StringBuilder();
            builder.Append(AuthSettings.BaseLoginUrl);
            builder.Append("?response_type=code+id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "https://{0}/", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", AuthSettings.AADAppId);
            builder.Append("&response_mode=fragment");
            builder.AppendFormat("&resource={0}", WebUtility.UrlEncode("https://management.core.windows.net/"));
            builder.AppendFormat("&site_id={0}", "500879");
            builder.AppendFormat("&nonce={0}", Guid.NewGuid());
            builder.Append(LoginStateUrlFragment(context));
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