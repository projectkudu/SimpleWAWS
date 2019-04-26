using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public class ReCaptchaAuthProvider : BaseOpenIdConnectAuthProvider
    {
        public override void AuthenticateRequest(HttpContextBase context)
        {
            base.AuthenticateRequest(context, TryAuthenticateReCaptchaRequest);
        }
        public override string GetLoginUrl(HttpContextBase context)
        {
            var builder = new StringBuilder();
            builder.Append(AuthSettings.ReCaptchaEndpoint);
            builder.AppendFormat("?redirect_uri={0}", WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "https://{0}/Login", context.Request.Headers["HOST"])));
            builder.Append(LoginStateUrlFragment(context));
            return builder.ToString();
        }

        protected override string GetValidAudience()
        {
            return AuthSettings.ReCaptchaSiteKey;
        }

        public override string GetIssuerName(string altSecId)
        {
            return "ReCaptcha";
        }
        protected TokenResults TryAuthenticateReCaptchaRequest(HttpContextBase context)
        {
            var code = context.Request.QueryString["code"];
            if (string.IsNullOrEmpty(code))
            {
                return TokenResults.DoesntExist;
            }

            var user = GetUserFromReCaptcha(code, context);

            if (user == null)
            {
                return TokenResults.ExistAndWrong;
            }
            context.User = user;
            return TokenResults.ExistsAndCorrect;
        }

        private IPrincipal GetUserFromReCaptcha(string unsplitcode, HttpContextBase context)
        {
            var code = unsplitcode.Split(new string[] { "|||" }, StringSplitOptions.RemoveEmptyEntries);
                var secretKey = Environment.GetEnvironmentVariable("ReCaptchaSecretKey");
                var remoteIP = GetIPAddress(context);
                var apiUrl = "https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}&remoteip={2}";
                var requestUri = string.Format(apiUrl, secretKey, code[0], remoteIP);
                var request = (HttpWebRequest)WebRequest.Create(requestUri);
                var isSuccess = false;
                var dateTime = String.Empty;
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                    {
                        JObject jResponse = JObject.Parse(stream.ReadToEnd());
                        isSuccess = jResponse.Value<bool>("success");
                        if (isSuccess)
                        {
                            dateTime = jResponse.Value<string>("challenge_ts");
                        }
                        else // check if we have seen this person/session before so we allow for the code to be reused by the same clientIP for an hour
                        {
                            if (!String.IsNullOrEmpty(code[1]) && !String.IsNullOrEmpty(code[2]))
                            {
                            try
                            {
                                if (code[1].Equals(remoteIP) && DateTime.UtcNow >= DateTime.ParseExact(code[2], "yyyy-MM-dd'T'HH:mm:ssZZ", System.Globalization.CultureInfo.InvariantCulture))
                                {
                                    isSuccess = true;
                                }
                            }
                            catch //TODO: trace this out 
                            { }
                            }
                        }
                     }
                }
                var datetimeUtcString = DateTime.Parse(dateTime).ToString("yyyy-MM-dd'T'HH:mm:ssZZ");
                return isSuccess ? new TryWebsitesPrincipal(new TryWebsitesIdentity($"{code[0]}|||{remoteIP}|||{datetimeUtcString}", $"{code[0]}|||{remoteIP}|||{datetimeUtcString}", "ReCaptcha")): null;
        }

        protected string GetIPAddress(HttpContextBase context)
        {
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }
            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        public override TokenResults TryAuthenticateBearer(HttpContextBase context)
        {
            var recaptchaBearer = GetBearer(context);

            if (recaptchaBearer == null)
            {
                return TokenResults.DoesntExist;
            }

            var user = GetUserFromReCaptcha(Crypto.DecryptStringAES(recaptchaBearer), context);

            if (user == null)
            {
                return TokenResults.ExistAndWrong;
            }

            context.User = user;
            return TokenResults.ExistsAndCorrect;
        }

        protected override string GetBearer(HttpContextBase context)
        {
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null || authHeader.IndexOf(AuthConstants.BearerHeader, StringComparison.OrdinalIgnoreCase) == -1) return null;
            return authHeader.Substring(AuthConstants.BearerHeader.Length).Trim();
        }
        public override bool HasToken(HttpContextBase context)
        {
            return context.Request.QueryString["code"] != null;
        }

    }
}