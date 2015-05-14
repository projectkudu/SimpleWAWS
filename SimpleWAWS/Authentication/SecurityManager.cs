using System.Configuration;
using System.Web;
using SimpleWAWS.Authentication;
using System.Collections.Generic;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using SimpleWAWS.Trace;
using System.Threading.Tasks;

namespace SimpleWAWS.Models
{
    public static class SecurityManager
    {
        private static readonly Dictionary<string, IAuthProvider> _authProviders =
            new Dictionary<string, IAuthProvider>(StringComparer.InvariantCultureIgnoreCase);

        private static string SelectedProvider(HttpContext context)
        {
            if (!string.IsNullOrEmpty(context.Request.QueryString["provider"]))
                return context.Request.QueryString["provider"];

            var state = context.Request.QueryString["state"];
            if (string.IsNullOrEmpty(state))
                return AuthConstants.DefaultAuthProvider;

            state = WebUtility.UrlDecode(state);
            var match = Regex.Match(state, "provider=([^&]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : AuthConstants.DefaultAuthProvider;
        }

        private static IAuthProvider GetAuthProvider(HttpContext context)
        {
            var requestedAuthProvider = SelectedProvider(context);

            IAuthProvider authProvider;
            if (_authProviders.TryGetValue(requestedAuthProvider, out authProvider))
            {
                return authProvider;
            }
            else
            {
                return _authProviders[AuthConstants.DefaultAuthProvider];
            }
        }

        public static void InitAuthProviders()
        {
            _authProviders.Add("AAD", new AADProvider());
            _authProviders.Add("Facebook", new FacebookAuthProvider());
            _authProviders.Add("Twitter", new TwitterAuthProvider());
            _authProviders.Add("Google", new GoogleAuthProvider());
        }

        public static void AuthenticateRequest(HttpContext context)
        {
            GetAuthProvider(context).AuthenticateRequest(context);
        }

        public static bool HasToken(HttpContext context)
        {
            return GetAuthProvider(context).HasToken(context);
        }

        public static bool IsAdmin(HttpContext context)
        {
            return context.User.Identity.Name == ConfigurationManager.AppSettings["AdminUserId"];
        }

        public static bool TryAuthenticateSessionCookie(HttpContext context)
        {
            try
            {
                var loginSessionCookie =
                    Uri.UnescapeDataString(context.Request.Cookies[AuthConstants.LoginSessionCookie].Value)
                        .Decrypt(AuthConstants.EncryptionReason);
                var splited = loginSessionCookie.Split(';');
                if (splited.Length == 2)
                {
                    var user = splited[0];
                    var date = DateTime.Parse(splited[1]);
                    if (ValidDateTimeSessionCookie(date))
                    {
                        context.User = new TryWebsitesPrincipal(new TryWebsitesIdentity(user, user, "Old"));
                        return true;
                    }
                }
                else if (splited.Length == 4)
                {
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
                else
                {
                    return false;
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
                SimpleTrace.Diagnostics.Error(e, "Exception during cookie authentication");
            }
            return false;
        }

        public static void HandleAnonymousUser(HttpContext context)
        {
            try
            {
                if (!context.IsBrowserRequest()) return;
                var userCookie = context.Request.Cookies[AuthConstants.AnonymousUser];
                if (userCookie == null)
                {
                    var user = Guid.NewGuid().ToString();
                    context.Response.Cookies.Add(new HttpCookie(AuthConstants.AnonymousUser, Uri.EscapeDataString(user.Encrypt(AuthConstants.EncryptionReason))) { Path = "/", Expires = DateTime.UtcNow.AddMinutes(30) });
                }
                else
                {
                    var user = Uri.UnescapeDataString(userCookie.Value).Decrypt(AuthConstants.EncryptionReason);
                    context.User = new TryWebsitesPrincipal(new TryWebsitesIdentity(user, null, "Anonymous"));
                }
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "Error Adding anonymous user");
            }
        }

        public static HttpResponseMessage RedirectToAAD(string redirectContext)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Forbidden);

            response.Headers.Add("LoginUrl", (_authProviders["AAD"] as AADProvider).GetLoginUrl(HttpContext.Current));

            if (HttpContext.Current.Response.Cookies[AuthConstants.LoginSessionCookie] != null)
            {
                response.Headers.AddCookies(new [] { new CookieHeaderValue(AuthConstants.LoginSessionCookie, string.Empty){ Expires = DateTime.UtcNow.AddDays(-1), Path = "/" } });
            }
            return response;
        }

        public static Task<HttpResponseMessage> AdminOnly(Func<Task<HttpResponseMessage>> func)
        {
            if (SecurityManager.IsAdmin(HttpContext.Current))
            {
                return func();
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden));
        }

        private static bool ValidDateTimeSessionCookie(DateTime date)
        {
            return date.Add(AuthConstants.SessionCookieValidTimeSpan) > DateTime.UtcNow;
        }
    }
}
