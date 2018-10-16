using SimpleWAWS.Code;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.IdentityModel.Claims;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

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

        abstract protected string GetValidAudience();
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
                ValidAudience = GetValidAudience(),
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
                var puidClaim = user.Claims.Where(c => c.Type == puidClaimType).Select(c => c.Value).FirstOrDefault();
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
            //a jwt token can either be in the query string or in the Authorization header or for AAD in the POST Form body
            var jwt = context.Request["id_token"];
            if (jwt != null) return jwt;
            if (context.Request.Form != null && context.Request.Form.Get("id_token") != null)
            {
                var jwtFromPost = context.Request.Form["id_token"];
                if (jwtFromPost != null) return jwtFromPost;
            }
            if (context.Request.Form != null && context.Request.Form.Get("code") != null)
            {
                var codeFromPost = context.Request.Form["code"];
                if (codeFromPost != null) return GetJwtFromCode(codeFromPost);
            }
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader == null || authHeader.IndexOf(AuthConstants.BearerHeader, StringComparison.OrdinalIgnoreCase) == -1) return null;
            return authHeader.Substring(AuthConstants.BearerHeader.Length).Trim();
        }

        public string GetTokenEndpoint(Guid tenantId)
        {
            return GetTokenEndpoint(tenantId.ToString());
        }

        public static string GetTokenEndpoint(string tenantName)
        {
            var token_endpoint = AuthSettings.AADTokenEndpoint;
            return token_endpoint;
            if (token_endpoint.Contains("/" + tenantName + "/"))
            {
                return token_endpoint;
            }
            return token_endpoint.Replace("/common/", String.Format("/{0}/", tenantName));
        }
        private string GetJwtFromCode(string codeFromPost)
        {
            // "token_endpoint":"https://login.windows-ppe.net/common/oauth2/token"
            var tokenRequestUri = GetTokenEndpoint(String.Empty) ;
            string jwt = GetJwt(audience: tokenRequestUri);
            var redirectUri = "https://localhost:44303";
            var payload = new StringBuilder("grant_type=authorization_code");
            payload.AppendFormat("&redirect_uri={0}", WebUtility.UrlEncode(SimpleWAWS.Models.Util.PunicodeUrl(redirectUri)));
            payload.AppendFormat("&code={0}", WebUtility.UrlEncode(codeFromPost));
            payload.AppendFormat("&client_assertion_type={0}", WebUtility.UrlEncode("urn:ietf:params:oauth:client-assertion-type:jwt-bearer"));
            payload.AppendFormat("&client_assertion={0}", WebUtility.UrlEncode(jwt));

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("client-request-id", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("User-Agent", "Try App Service");

                using (var response =  client.PostAsync(tokenRequestUri, content).GetAwaiter().GetResult())
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return HandleOAuthError(response, tokenRequestUri).GetAwaiter().GetResult();
                    }

                    var result = response.Content.ReadAsAsync<AADOAuth2AccessToken>().GetAwaiter().GetResult();
                    //result.TenantId = tenantId != "common" ? tenantId : GetTenantIdFromToken(result.access_token);
                    return result.access_token;
                }
            }
            return null;
        }
        static string GetJwt(string audience)
        {

            string clientId = "72f";
            var claims = new List<System.Security.Claims.Claim>();
            claims.Add(new System.Security.Claims.Claim("sub", clientId));
            claims.Add(new System.Security.Claims.Claim("jti", Guid.NewGuid().ToString())); // RFC 7519: https://tools.ietf.org/html/rfc7519#section-4.1.7

            var handler = new JwtSecurityTokenHandler();
            //TODO: Switch over to load from Machine CertCollection
            var credentials = new X509SigningCredentials(new System.Security.Cryptography.X509Certificates.X509Certificate2(HostingEnvironment.MapPath("~/App_Data/"), AuthSettings.AADAppCertificatePassword,System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet));
            return handler.CreateToken(clientId, audience, new ClaimsIdentity(claims), null, credentials).RawData;
        }

        static async Task<string> HandleOAuthError(HttpResponseMessage response, string requestUri)
        {
            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                var error = await response.Content.ReadAsAsync<AADOAuth2Error>();
                if (error != null && !String.IsNullOrEmpty(error.error_description))
                {
                    SimpleTrace.TraceError((String.Format("AUTH POST failed with {0}  POST {1}", response.StatusCode, requestUri)));
                    return null;
                }
            }
            SimpleTrace.TraceError((String.Format("AUTH POST failed with {0}  POST {1}", response.StatusCode, requestUri)));
            return null;
        }

    }
    public class AADOAuth2Error
    {
        public string error { get; set; }
        public string error_description { get; set; }
    }
    public class AADOAuth2AccessToken
    {
        public string expires_on { get; set; }
        public string resource { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string TenantId { get; set; }
    }
}
