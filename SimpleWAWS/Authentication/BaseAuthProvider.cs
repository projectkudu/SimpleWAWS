﻿using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Globalization;
using System.Net;
using System.Security.Principal;
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
                        if (context.IsAjaxRequest() || context.IsClientPortalBackendRequest())
                        {
                            //context.Response.Headers["LoginUrl"] = GetLoginUrl(context);
                            context.Response.Headers["loginurl"] = GetLoginUrl(context);
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
                        context.Response.RedirectLocation = GetRedirectLocationFromState(context);
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
            SimpleTrace.TraceInformation("{0}", AnalyticsEvents.OldUserLoggedIn);
            try
            {
                var anonymousUser = HttpContext.Current.Request.Cookies[AuthConstants.AnonymousUser];
                if (anonymousUser != null)
                {
                    SimpleTrace.TraceInformation("{0}",AnalyticsEvents.AnonymousUserLogedIn);
                }
            }
            catch
            { }
            return new HttpCookie(AuthConstants.LoginSessionCookie, GetSessionCookieString(user)) { Path = "/", Expires = DateTime.UtcNow.AddHours(1) };
        }

        public string GetRedirectLocationFromState(HttpContextBase context)
        {
            if (context.Request["state"].Contains("appServiceName=Function")|| context.Request["state"].Contains("appServiceName=VsCodeLinux"))
            {
                var cookie = GetSessionCookieString(context.User);
                var state = context.Request["state"];
                var redirectlocation = state.Split('?')[0];
                return $"{redirectlocation}?cookie={cookie}&state={Uri.EscapeDataString(state)}";
            }
            else if (context.Request.Form !=null && context.Request.Form.Get("state") != null && (context.Request.Form["state"].Contains("appServiceName=Function")|| context.Request.Form["state"].Contains("appServiceName=VSCodeLinux")))
            {
                var cookie = GetSessionCookieString(context.User);
                var state = context.Request.Form["state"];
                var redirectlocation = state.Split('?')[0];
                return $"{redirectlocation}?cookie={cookie}&state={Uri.EscapeDataString(state)}";
            }
            else
            {
                return context.Request["state"];
            }
        }
        public string GetSessionCookieString(IPrincipal user)
        {
            var identity = user.Identity as TryWebsitesIdentity;
            var value = string.Format(CultureInfo.InvariantCulture, "{0};{1};{2};{3}", identity.Email, identity.Puid, identity.Issuer, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            return Uri.EscapeDataString(Crypto.EncryptStringAES(value));
        }

        protected string LoginStateUrlFragment(HttpContextBase context, bool encodeTwice = false)
        {
            if (context.IsClientPortalBackendRequest())
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
