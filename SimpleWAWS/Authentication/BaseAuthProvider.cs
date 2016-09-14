using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public abstract class BaseAuthProvider : IAuthProvider
    {
        public abstract void AuthenticateRequest(HttpContextBase context);
        public abstract bool HasToken(HttpContextBase context);
        public abstract string GetLoginUrl(HttpContextBase context);
        protected void AuthenticateRequest(HttpContextBase context, Func<HttpContextBase, TokenResults> providerSpecificAuthMethod)
        {
            try
            {
                switch (providerSpecificAuthMethod(context))
                {
                    case TokenResults.DoesntExist:
                        if (context.IsAjaxRequest() || context.IsFunctionsPortalBackendRequest())
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
                        context.Response.RedirectLocation = AuthSettings.LoginErrorPage;
                        context.Response.StatusCode = 302; // Redirect
                        break;
                    case TokenResults.ExistsAndCorrect:
                        // Ajax can never send Bearer token
                        context.Response.Cookies.Add(CreateSessionCookie(context.User));
                        context.Response.RedirectLocation = SecurityManager.GetRedirectLocationFromState(context);
                        context.Response.StatusCode = 302; // Redirect
                        break;
                    default:
                        //this should never happen
                        break;
                }
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "General Authentication Exception");
                context.Response.RedirectLocation = AuthSettings.LoginErrorPage;
                context.Response.StatusCode = 302; // Redirect
            }
            finally
            {
                context.Response.End();
            }
        }

        public HttpCookie CreateSessionCookie(IPrincipal user)
        {
            var identity = user.Identity as TryWebsitesIdentity;
            var value = string.Format(CultureInfo.InvariantCulture, "{0};{1};{2};{3}", identity.Email, identity.Puid, identity.Issuer, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            SimpleTrace.Analytics.Information(AnalyticsEvents.UserLoggedIn, identity);
            SimpleTrace.TraceInformation("{0}; {1}; {2}", AnalyticsEvents.OldUserLoggedIn, identity.Email, identity.Issuer);
            try
            {
                var anonymousUser = HttpContext.Current.Request.Cookies[AuthConstants.AnonymousUser];
                if (anonymousUser != null)
                {
                    var anonymousIdentity = new TryWebsitesIdentity(Uri.UnescapeDataString(anonymousUser.Value).Decrypt(AuthConstants.EncryptionReason), null, "Anonymous");
                    SimpleTrace.TraceInformation("{0}; {1}; {2}",
                        AnalyticsEvents.AnonymousUserLogedIn,
                        anonymousIdentity.Name,
                        identity.Name);
                    SimpleTrace.AnonymousUserLoggedIn(anonymousIdentity, identity);
                }
            }
            catch
            { }
            return new HttpCookie(AuthConstants.LoginSessionCookie, Uri.EscapeDataString(value.Encrypt(AuthConstants.EncryptionReason))) { Path = "/", Expires = DateTime.UtcNow.AddDays(2) };
        }


        protected string LoginStateUrlFragment(HttpContextBase context, bool encodeTwice = false)
        {
            if (context.IsFunctionsPortalBackendRequest())
            {
                return encodeTwice ? 
                      $"&state={WebUtility.UrlEncode(WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}{1}", context.Request.Headers["Referer"], context.Request.Url.Query)))}" 
                    : $"&state={WebUtility.UrlEncode(string.Format(CultureInfo.InvariantCulture, "{0}{1}", context.Request.Headers["Referer"], context.Request.Url.Query))}";
            }
            else
            {
                var culture = CultureInfo.CurrentCulture.Name.ToLowerInvariant();
                return encodeTwice ? 
                    $"&state={WebUtility.UrlEncode(WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format(CultureInfo.InvariantCulture, "{0}{1}", culture, context.Request.Url.Query) : context.Request.Url.PathAndQuery))}" 
                  : $"&state={WebUtility.UrlEncode(context.IsAjaxRequest() ? string.Format(CultureInfo.InvariantCulture, "{0}{1}", culture, context.Request.Url.Query) : context.Request.Url.PathAndQuery)}";
            }
        }
    }
}
