using SimpleWAWS.Code;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public abstract class BaseAuthProvider : IAuthProvider
    {
        private const string emailClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        private const string upnClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
        private const string nameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string issuerClaimType = "iss";
        private const string puidClaimType = "puid";
        private const string altSecIdClaimType = "altsecid";

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

        abstract protected string GetLoginUrl(HttpContext context);
        abstract protected string GetValidAudiance();


        protected TokenResults TryAuthenticateBearer(HttpContext context)
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

        private IPrincipal ValidateJWT(string jwt)
        {
            var handler = new JwtSecurityTokenHandler { CertificateValidator = X509CertificateValidator.None };
            if (!handler.CanReadToken(jwt))
            {
                return null;
            }

            var parameters = new TokenValidationParameters
            {
                ValidAudience = GetValidAudiance(),
                ValidateIssuer = false,
                IssuerSigningTokens = OpenIdConfiguration.GetIssuerSigningKeys(jwt)
            };

            try
            {
                var user = handler.ValidateToken(jwt, parameters);
                var upnClaim = user.Claims.Where(c => c.Type == upnClaimType).Select(c => c.Value).FirstOrDefault();
                var emailClaim = user.Claims.Where(c => c.Type == emailClaimType).Select(c => c.Value).FirstOrDefault();
                var nameClaim = user.Claims.Where(c => c.Type == nameClaimType).Select(c => c.Value).FirstOrDefault();
                var issuerClaim = user.Claims.Where(c => c.Type == issuerClaimType).Select(c => c.Value).FirstOrDefault();
                var puidClaim = user.Claims.Where(c => c.Type == puidClaimType || c.Type == altSecIdClaimType).Select(c => c.Value).FirstOrDefault();

                if (puidClaim != null)
                {
                    Trace.TraceInformation("{0}; {1}; {2}", AnalyticsEvents.UserPuidValue, user.Identity.Name, puidClaim.Split(':').Last());
                }

                return new TryWebsitesPrincipal(new TryWebsitesIdentity(upnClaim ?? emailClaim ?? user.Identity.Name, puidClaim, issuerClaim));
            }
            catch (Exception e)
            {
                //failed validating
                Trace.TraceError(e.ToString());
            }

            return null;
        }

        protected HttpCookie CreateSessionCookie(IPrincipal user)
        {
            var identity = user.Identity as TryWebsitesIdentity;
            var value = string.Format("{0};{1};{2};{3}", identity.Email, identity.Puid, identity.Issuer, DateTime.UtcNow);
            Trace.TraceInformation("{0};{1};{2}", AnalyticsEvents.UserLoggedIn, identity.Email, identity.Issuer);
            return new HttpCookie(Constants.LoginSessionCookie, Uri.EscapeDataString(value.Encrypt(Constants.EncryptionReason))) { Path = "/" };
        }

        protected bool TryAuthenticateSessionCookie(HttpContext context)
        {
            try
            {
                var loginSessionCookie =
                    Uri.UnescapeDataString(context.Request.Cookies[Constants.LoginSessionCookie].Value)
                        .Decrypt(Constants.EncryptionReason);
                var splited = loginSessionCookie.Split(';');
                var date = DateTime.Parse(loginSessionCookie.Split(';')[3]);
                if (ValidDateTimeSessionCookie(date))
                {
                    var email = splited[0];
                    var puid = splited[1];
                    var issuer = splited[2];
                    context.User = new TryWebsitesPrincipal(new TryWebsitesIdentity(email, puid, issuer));
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

        protected string GetBearer(HttpContext context)
        {
            //a jwt token can either be in the query string or in the Authorization header
            var jwt = context.Request["id_token"];
            if (jwt != null) return jwt;
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null || authHeader.IndexOf(Constants.BearerHeader, StringComparison.InvariantCultureIgnoreCase) == -1) return null;
            return authHeader.Substring(Constants.BearerHeader.Length).Trim();
        }

        protected bool ValidDateTimeSessionCookie(DateTime date)
        {
            return date.Add(Constants.SessionCookieValidTimeSpan) > DateTime.UtcNow;
        }
    }
}