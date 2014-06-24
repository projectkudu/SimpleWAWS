using System;
using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Selectors;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.IdentityModel.Tokens;
using SimpleWAWS.Code;

namespace SimpleWAWS.Authentication
{
    public class AADProvider : IAuthProvider
    {
        public void AuthenticateRequest(HttpContext context)
        {
            if (!TryAuthenticateSessionCookie(context)
                && !TryAuthenticateBarrer(context))
            {
                AuthenticateAAD(context);
            }
        }

        public void HandleCallBack(HttpContext context)
        {
            if (TryAuthenticateBarrer(context))
            {
                context.Response.Cookies.Add(CreateSessionCookie(context.User));
                context.Response.RedirectLocation = context.Request["state"];
                context.Response.StatusCode = 302; //Redirect
            }
            else
            {
                context.Response.StatusCode = 403; //Forbidden
            }
            context.Response.End();
        }

        public bool TryAuthenticateBarrer(HttpContext context, string jwt = null)
        {
            jwt = jwt ?? GetBearer(context);

            if (jwt == null)
            {
                return false;
            }

            var user = ValidateJWT(jwt);

            if (user == null)
            {
                return false;
            }

            context.User = user;
            return true;
        }

        public HttpCookie CreateSessionCookie(IPrincipal user)
        {
            var value = string.Format("{0};{1}", user.Identity.Name, DateTime.UtcNow);
            return new HttpCookie(Constants.LoginSessionCookie, Uri.EscapeDataString(value.Encrypt(Constants.EncryptionReason))) {Path = "/"};
        }

        public bool TryAuthenticateSessionCookie(HttpContext context)
        {
            try
            {
                var loginSessionCookie = Uri.UnescapeDataString(context.Request.Cookies[Constants.LoginSessionCookie].Value).Decrypt(Constants.EncryptionReason);
                var user = loginSessionCookie.Split(';')[0];
                var date = DateTime.Parse(loginSessionCookie.Split(';')[1]);
                if (ValidDateTimeSessionCookie(date))
                {
                    context.User = new SimplePrincipal(new SimpleIdentity(user, "MSA"));
                    return true;
                }
            }
            catch (Exception e)
            {
                // we need to authenticate
                //TODO: log the error 
                Trace.TraceError(e.ToString());
            }
            return false;
        }

        public void AuthenticateAAD(HttpContext context)
        {
            context.Response.Redirect(GetLoginUrl(context), endResponse: true);
        }

        private string GetLoginUrl(HttpContext context)
        {
            var builder = new StringBuilder();
            builder.Append(ConfigurationManager.AppSettings[Constants.BaseLoginUrl]);
            builder.Append("?response_type=id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode("https://" + context.Request.Headers["HOST"] + ConfigurationManager.AppSettings[Constants.RedirectUrl]));
            builder.AppendFormat("&client_id={0}", ConfigurationManager.AppSettings[Constants.AADAppId]);
            builder.AppendFormat("&response_mode=query");
            builder.AppendFormat("&nonce={0}", Guid.NewGuid());
            builder.AppendFormat("&state={0}", WebUtility.UrlEncode("/"));
            return builder.ToString();
        }

        private IPrincipal ValidateJWT(string jwt)
        {
            var handler = new JwtSecurityTokenHandler { CertificateValidator = X509CertificateValidator.None };
            if (!handler.CanReadToken(jwt))
            {
                return null;
            }
            var parameters = new TokenValidationParameters
            {
                ValidAudience = ConfigurationManager.AppSettings[Constants.AADAppId],
                ValidateIssuer = false,
                IssuerSigningTokens = OpenIdConfiguration.GetIssuerSigningKeys(jwt)
            };
            var user = handler.ValidateToken(jwt, parameters);
            return user;
        }

        private string GetBearer(HttpContext context)
        {
            //a jwt token can either be in the query string or in the Authorization header
            var jwt = context.Request["id_token"];
            if (jwt != null) return jwt;
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null || authHeader.IndexOf(Constants.BearerHeader, StringComparison.InvariantCultureIgnoreCase) == -1) return null;
            return authHeader.Substring(Constants.BearerHeader.Length).Trim();
        }

        private bool ValidDateTimeSessionCookie(DateTime date)
        {
            return date < DateTime.UtcNow.Add(Constants.SessionCookieValidTimeSpan);
        }
    }
}