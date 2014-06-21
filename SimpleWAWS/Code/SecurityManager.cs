using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IdentityModel.Selectors;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.IdentityModel.Tokens;

namespace SimpleWAWS.Code
{
    public static class SecurityManager
    {
        private static class SecurityConstants
        {
            public const string EncryptionReason = "ProtectCookie";
            public const string LoginSessionCookie = "loginsession";
            public const string BaseLoginUrl = "BaseLoginUrl";
            public const string RedirectUrl = "RedirectUrl";
            public const string AADAppId = "AADAppId";
        }

        private const int SessionCookieHoursValid = 8;

        public static bool TryAuthenticateBarrer(HttpContext context, string jwt = null)
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

        public static CookieHeaderValue CreateSessionCookie(IPrincipal user)
        {
            var value = string.Format("{0};{1}", user.Identity.Name, DateTime.UtcNow);
            return new CookieHeaderValue(SecurityConstants.LoginSessionCookie, value.Encrypt(SecurityConstants.EncryptionReason)) { Path = "/" };
        }

        public static bool TryAuthenticateSessionCookie(HttpContext context)
        {
            try
            {
                var loginSessionCookie = WebUtility.UrlDecode(context.Request.Cookies[SecurityConstants.LoginSessionCookie].Value).Decrypt(SecurityConstants.EncryptionReason);
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

        public static void AuthenticateAAD(HttpContext context)
        {
            context.Response.Redirect(GetLoginUrl(context), endResponse: true);
        }

        private static string GetLoginUrl(HttpContext context)
        {
            var builder = new StringBuilder();
            builder.Append(ConfigurationManager.AppSettings[SecurityConstants.BaseLoginUrl]);
            builder.Append("?response_type=id_token");
            builder.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode("https://" + context.Request.Headers["HOST"] + ConfigurationManager.AppSettings[SecurityConstants.RedirectUrl]));
            builder.AppendFormat("&client_id={0}", ConfigurationManager.AppSettings[SecurityConstants.AADAppId]);
            builder.AppendFormat("&response_mode=query");
            builder.AppendFormat("&nonce={0}", Guid.NewGuid());
            return builder.ToString();
        }

        private static IPrincipal ValidateJWT(string bearer)
        {
            var handler = new JwtSecurityTokenHandler {CertificateValidator = X509CertificateValidator.None};
            if (!handler.CanReadToken(bearer))
            {
                return null;
            }
            var parameters = new TokenValidationParameters();
            parameters.ValidAudience = ConfigurationManager.AppSettings[SecurityConstants.AADAppId];
            parameters.ValidateIssuer = false;
            parameters.IssuerSigningTokens = GetIssuerTokens();
            var user = handler.ValidateToken(bearer, parameters);
            return user;
        }

        private static IEnumerable<SecurityToken> GetIssuerTokens()
        {
            yield return
                new X509SecurityToken(
                    new X509Certificate2(
                        Convert.FromBase64String(
                            "MIIDPjCCAiqgAwIBAgIQsRiM0jheFZhKk49YD0SK1TAJBgUrDgMCHQUAMC0xKzApBgNVBAMTImFjY291bnRzLmFjY2Vzc2NvbnRyb2wud2luZG93cy5uZXQwHhcNMTQwMTAxMDcwMDAwWhcNMTYwMTAxMDcwMDAwWjAtMSswKQYDVQQDEyJhY2NvdW50cy5hY2Nlc3Njb250cm9sLndpbmRvd3MubmV0MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAkSCWg6q9iYxvJE2NIhSyOiKvqoWCO2GFipgH0sTSAs5FalHQosk9ZNTztX0ywS/AHsBeQPqYygfYVJL6/EgzVuwRk5txr9e3n1uml94fLyq/AXbwo9yAduf4dCHTP8CWR1dnDR+Qnz/4PYlWVEuuHHONOw/blbfdMjhY+C/BYM2E3pRxbohBb3x//CfueV7ddz2LYiH3wjz0QS/7kjPiNCsXcNyKQEOTkbHFi3mu0u13SQwNddhcynd/GTgWN8A+6SN1r4hzpjFKFLbZnBt77ACSiYx+IHK4Mp+NaVEi5wQtSsjQtI++XsokxRDqYLwus1I1SihgbV/STTg5enufuwIDAQABo2IwYDBeBgNVHQEEVzBVgBDLebM6bK3BjWGqIBrBNFeNoS8wLTErMCkGA1UEAxMiYWNjb3VudHMuYWNjZXNzY29udHJvbC53aW5kb3dzLm5ldIIQsRiM0jheFZhKk49YD0SK1TAJBgUrDgMCHQUAA4IBAQCJ4JApryF77EKC4zF5bUaBLQHQ1PNtA1uMDbdNVGKCmSf8M65b8h0NwlIjGGGy/unK8P6jWFdm5IlZ0YPTOgzcRZguXDPj7ajyvlVEQ2K2ICvTYiRQqrOhEhZMSSZsTKXFVwNfW6ADDkN3bvVOVbtpty+nBY5UqnI7xbcoHLZ4wYD251uj5+lo13YLnsVrmQ16NCBYq2nQFNPuNJw6t3XUbwBHXpF46aLT1/eGf/7Xx6iy8yPJX4DyrpFTutDz882RWofGEO5t4Cw+zZg70dJ/hH/ODYRMorfXEW+8uKmXMKmX2wyxMKvfiPbTy5LmAU8Jvjs2tLg4rOBcXWLAIarZ")));
        }

        private static string GetBearer(HttpContext context)
        {
            return null;
        }

        private static bool ValidDateTimeSessionCookie(DateTime date)
        {
            return date < DateTime.UtcNow.AddHours(SessionCookieHoursValid);
        }
    }
}