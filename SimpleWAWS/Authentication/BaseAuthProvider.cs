using SimpleWAWS.Code;
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
        protected abstract string GetLoginUrl(HttpContext context);

        protected void AuthenticateRequest(HttpContext context, Func<HttpContext, TokenResults> providerSpecificAuthMethod)
        {
            try
            {
                if (!TryAuthenticateSessionCookie(context))
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
            catch(Exception e)
            {
                if (e is ThreadAbortException || e.GetBaseException() is ThreadAbortException) return;
                Trace.TraceError(e.ToString());
                context.Response.RedirectLocation = ConfigurationManager.AppSettings["LoginErrorPage"];
                context.Response.StatusCode = 302; // Redirect
            }
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

        protected HttpCookie CreateSessionCookie(IPrincipal user)
        {
            var identity = user.Identity as TryWebsitesIdentity;
            var value = string.Format("{0};{1};{2};{3}", identity.Email, identity.Puid, identity.Issuer, DateTime.UtcNow);
            Trace.TraceInformation("{0};{1};{2}", AnalyticsEvents.UserLoggedIn, identity.Email, identity.Issuer);
            return new HttpCookie(Constants.LoginSessionCookie, Uri.EscapeDataString(value.Encrypt(Constants.EncryptionReason))) { Path = "/" };
        }

        protected bool ValidDateTimeSessionCookie(DateTime date)
        {
            return date.Add(Constants.SessionCookieValidTimeSpan) > DateTime.UtcNow;
        }

    }
}