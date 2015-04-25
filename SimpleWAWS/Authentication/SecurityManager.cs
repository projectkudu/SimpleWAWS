using System.Configuration;
using System.Web;
using SimpleWAWS.Authentication;
using System.Collections.Generic;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace SimpleWAWS.Code
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
                return Constants.DefaultAuthProvider;

            state = WebUtility.UrlDecode(state);
            var match = Regex.Match(state, "provider=([^&]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : Constants.DefaultAuthProvider;
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
                return _authProviders[Constants.DefaultAuthProvider];
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

        public static void EnsureAdmin(HttpContext context)
        {
            if (context.User.Identity.Name != ConfigurationManager.AppSettings["AdminUserId"])
            {
                context.Response.StatusCode = 403; //Forbidden
                context.Response.End();
            }
        }

        public static bool TryAuthenticateSessionCookie(HttpContext context)
        {
            try
            {
                var loginSessionCookie =
                    Uri.UnescapeDataString(context.Request.Cookies[Constants.LoginSessionCookie].Value)
                        .Decrypt(Constants.EncryptionReason);
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
                Trace.TraceError(e.ToString());
            }
            return false;
        }

        public static void HandleAnonymousUser(HttpContext context)
        {
            try
            {
                var userCookie = context.Request.Cookies[Constants.AnonymousUser];
                var user = string.Empty;
                if (userCookie == null)
                {
                    user = Guid.NewGuid().ToString();
                    context.Response.Cookies.Add(new HttpCookie(Constants.AnonymousUser, Uri.EscapeDataString(user.Encrypt(Constants.EncryptionReason))) { Path = "/", Expires = DateTime.UtcNow.AddDays(1) });
                }
                else
                {
                    user = Uri.UnescapeDataString(userCookie.Value).Decrypt(Constants.EncryptionReason);
                }
                context.User = new TryWebsitesPrincipal(new TryWebsitesIdentity(user, null, "Anonymous"));
            }
            catch (Exception e)
            {
                Trace.TraceError("Error Adding anonymous user: " + e.ToString());
            }
        }

        private static bool ValidDateTimeSessionCookie(DateTime date)
        {
            return date.Add(Constants.SessionCookieValidTimeSpan) > DateTime.UtcNow;
        }
    }
}