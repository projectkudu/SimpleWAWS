using System.Globalization;
using System.Net;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class GoogleAuthProvider : BaseOpenIdConnectAuthProvider
    {
        public override string GetLoginUrl(HttpContextBase context)
        {
            var builder = new StringBuilder();
            builder.Append("https://accounts.google.com/o/oauth2/auth");
            builder.Append("?response_type=id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "https://{0}/Login", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", AuthSettings.GoogleAppId);
            builder.AppendFormat("&scope={0}", "email");
            builder.Append(LoginStateUrlFragment(context));
            return builder.ToString();
        }

        protected override string GetValidAudience()
        {
            return AuthSettings.GoogleAppId;
        }

        public override string GetIssuerName(string altSecId)
        {
            return "Google";
        }
    }
}