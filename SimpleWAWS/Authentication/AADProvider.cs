using System;
using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Selectors;
using System.Linq;
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
            if (!TryAuthenticateSessionCookie(context))
            {
                switch (TryAuthenticateBearer(context))
                {
                    case TokenResults.DoesntExist:
                        if (context.IsAjaxRequest())
                        {
                            context.Response.Headers["LoginUrl"] = GetLoginUrl(context);
                            context.Response.StatusCode = 403; // Forbidden
                        }
                        else
                        {
                            context.Response.RedirectLocation = GetLoginUrl(context);
                            context.Response.StatusCode = 302; // Redirect
                        }
                        break;
                    case TokenResults.ExistAndWrong:
                        // Ajax can never send an invalid Bearer token
                        context.Response.RedirectLocation = ConfigurationManager.AppSettings["LoginErrorPage"];
                        context.Response.StatusCode = 302; // Redirect
                        break;
                    case TokenResults.ExistsAndCorrect:
                        // Ajax can never send Bearer token
                        context.Response.Cookies.Add(CreateSessionCookie(context.User));
                        context.Response.RedirectLocation = context.Request["state"];
                        context.Response.StatusCode = 302; // Redirect
                        break;
                    default:
                        //this should never happen
                        break;
                }
                context.Response.End();
            }
        }

        public bool HasToken(HttpContext context)
        {
            return GetBearer(context) != null;
        }

        private TokenResults TryAuthenticateBearer(HttpContext context)
        {
            var jwt = GetBearer(context);

            if (jwt == null)
            {
                return TokenResults.DoesntExist;
            }

            var user = ValidateJWT(jwt);

            if (user == null)
            {
                return TokenResults.ExistAndWrong;
            }

            context.User = user;
            return TokenResults.ExistsAndCorrect;
        }

        private HttpCookie CreateSessionCookie(IPrincipal user)
        {
            var value = string.Format("{0};{1}", user.Identity.Name, DateTime.UtcNow);
            Trace.TraceInformation("{0}; {1}", AnalyticsEvents.UserLoggedIn, user.Identity.Name);
            return new HttpCookie(Constants.LoginSessionCookie, Uri.EscapeDataString(value.Encrypt(Constants.EncryptionReason))) { Path = "/" };
        }

        private bool TryAuthenticateSessionCookie(HttpContext context)
        {
            try
            {
                var loginSessionCookie =
                    Uri.UnescapeDataString(context.Request.Cookies[Constants.LoginSessionCookie].Value)
                        .Decrypt(Constants.EncryptionReason);
                var user = loginSessionCookie.Split(';')[0];
                var date = DateTime.Parse(loginSessionCookie.Split(';')[1]);
                if (ValidDateTimeSessionCookie(date))
                {
                    context.User = new SimplePrincipal(new SimpleIdentity(user, "MSA"));
                    return true;
                }
            }
            catch (NullReferenceException)
            {
                // we need to authenticate
            }
            catch (Exception e)
            {
                // we need to authenticate
                //but also log the error
                Trace.TraceError(e.ToString());
            }
            return false;
        }

        private string GetLoginUrl(HttpContext context)
        {
            var builder = new StringBuilder();
            builder.Append(ConfigurationManager.AppSettings[Constants.BaseLoginUrl]);
            builder.Append("?response_type=id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(string.Format("https://{0}/", context.Request.Headers["HOST"])));
            builder.AppendFormat("&client_id={0}", ConfigurationManager.AppSettings[Constants.AADAppId]);
            builder.AppendFormat("&response_mode=query");
            builder.AppendFormat("&resource={0}", WebUtility.UrlEncode("https://management.core.windows.net/"));
            builder.AppendFormat("&site_id={0}", "500879");
            builder.AppendFormat("&nonce={0}", Guid.NewGuid());
            builder.AppendFormat("&state={0}", WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format("/{0}", context.Request.Url.Query) : context.Request.Url.PathAndQuery));
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

            try
            {
                var user = handler.ValidateToken(jwt, parameters);
                var puidClaim = user.Claims.FirstOrDefault(c => c.Type == "puid" || c.Type == "altsecid");
                if (puidClaim != null && puidClaim.Value != null)
                {
                    Trace.TraceInformation("{0}; {1}; {2}", AnalyticsEvents.UserPuidValue, user.Identity.Name, puidClaim.Value.Split(':').Last());
                }
                return user;
            }
            catch (Exception e)
            {
                //failed validating
                Trace.TraceError(e.ToString());
            }

            return null;
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
            return date.Add(Constants.SessionCookieValidTimeSpan) > DateTime.UtcNow;
        }

        private enum TokenResults
        {
            DoesntExist,
            ExistAndWrong,
            ExistsAndCorrect
        }
    }
}