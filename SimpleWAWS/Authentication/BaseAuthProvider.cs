using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Web;

namespace SimpleWAWS.Authentication
{
    public abstract class BaseAuthProvider : IAuthProvider
    {
        public abstract void AuthenticateRequest(HttpContext context);
        public abstract bool HasToken(HttpContext context);
        public abstract string GetLoginUrl(HttpContext context);

        protected void AuthenticateRequest(HttpContext context, Func<HttpContext, TokenResults> providerSpecificAuthMethod)
        {
            try
            {
                switch (providerSpecificAuthMethod(context))
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
                        context.Response.RedirectLocation = AuthSettings.LoginErrorPage;
                        context.Response.StatusCode = 302; // Redirect
                        break;
                    case TokenResults.ExistsAndCorrect:
                        // Ajax can never send Bearer token
                        context.Response.Cookies.Add(CreateSessionCookie(context.User));
                        if (context.Response.Cookies[AuthConstants.AnonymousUser] != null)
                        {
                            context.Response.Cookies[AuthConstants.AnonymousUser].Expires = DateTime.UtcNow.AddDays(-1);
                        }
                        context.Response.RedirectLocation = context.Request["state"];
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

        protected HttpCookie CreateSessionCookie(IPrincipal user)
        {
            var identity = user.Identity as TryWebsitesIdentity;
            var value = string.Format("{0};{1};{2};{3}", identity.Email, identity.Puid, identity.Issuer, DateTime.UtcNow);
            SimpleTrace.Analytics.Information(AnalyticsEvents.UserLoggedIn, identity);
            SimpleTrace.TraceInformation("{0};{1};{2}", AnalyticsEvents.OldUserLoggedIn, identity.Email, identity.Issuer);
            return new HttpCookie(AuthConstants.LoginSessionCookie, Uri.EscapeDataString(value.Encrypt(AuthConstants.EncryptionReason))) { Path = "/" };
        }
    }
}