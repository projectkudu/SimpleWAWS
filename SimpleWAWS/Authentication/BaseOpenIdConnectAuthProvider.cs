using SimpleWAWS.Models;
using SimpleWAWS.Trace;
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
    public abstract class BaseOpenIdConnectAuthProvider : BaseAuthProvider
    {
        private const string emailClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
        private const string upnClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
        private const string nameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
        private const string issuerClaimType = "iss";
        private const string puidClaimType = "puid";
        private const string altSecIdClaimType = "altsecid";

        public override void AuthenticateRequest(HttpContextBase context)
        {
            base.AuthenticateRequest(context, TryAuthenticateBearer);
        }

        public override bool HasToken(HttpContextBase context)
        {
            return GetBearer(context) != null;
        }

        abstract protected string GetValidAudiance();
        abstract public string GetIssuerName(string altSecId);


        public TokenResults TryAuthenticateBearer(HttpContextBase context)
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
                ValidateAudience = false,
                IssuerSigningTokens = OpenIdConfiguration.GetIssuerSigningKeys(jwt)
            };

            try
            {
                var user = handler.ValidateToken(jwt, parameters);
                var upnClaim = user.Claims.Where(c => c.Type == upnClaimType).Select(c => c.Value).FirstOrDefault();
                var emailClaim = user.Claims.Where(c => c.Type == emailClaimType).Select(c => c.Value).FirstOrDefault();
                var nameClaim = user.Claims.Where(c => c.Type == nameClaimType).Select(c => c.Value).FirstOrDefault();
                var issuerClaim = user.Claims.Where(c => c.Type == issuerClaimType).Select(c => c.Value).FirstOrDefault();
                var puidClaim = user.Claims.Where(c => c.Type == puidClaimType ).Select(c => c.Value).FirstOrDefault();
                var altSecId = user.Claims.Where(c => c.Type == altSecIdClaimType).Select(c => c.Value).FirstOrDefault();
                var principal = new TryWebsitesPrincipal(new TryWebsitesIdentity(upnClaim ?? emailClaim ?? user.Identity.Name, altSecId ?? puidClaim, GetIssuerName(altSecId ?? puidClaim)));

                return principal;
            }
            catch (Exception e)
            {
                //failed validating
                SimpleTrace.Diagnostics.Error(e, "Error reading claims {jwt}", jwt);
            }

            return null;
        }

        protected string GetBearer(HttpContextBase context)
        {
            //a jwt token can either be in the query string or in the Authorization header
            var jwt = context.Request["id_token"];
            if (jwt != null) return jwt;
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null || authHeader.IndexOf(AuthConstants.BearerHeader, StringComparison.OrdinalIgnoreCase) == -1) return null;
            return authHeader.Substring(AuthConstants.BearerHeader.Length).Trim();
        }
    }
}