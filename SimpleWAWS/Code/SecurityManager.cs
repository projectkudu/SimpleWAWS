using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using System.Web.Security;
using WindowsLive;

namespace SimpleWAWS.Code
{
    public static class SecurityManager
    {
        private const string EncryptionReason = "ProtectCookie";
        private const string LoginSessionCookie = "loginsession";
        private static readonly WindowsLiveLogin WindowsLiveLogin = new WindowsLiveLogin(true);
        private const int SessionCookieHoursValid = 8;
        public static void ValidateAuthentication(HttpContext context)
        {
            if (context.Request.Path == "/api/Login")
                return;
            ValidateLoginCookie(context);
        }

        private static void ValidateLoginCookie(HttpContext context)
        {
            try
            {
                var loginSessionCookie = context.Request.Cookies[LoginSessionCookie];
                var encryptedValue = loginSessionCookie.Value;

                //TODO: handle crypto exception
                var decryptedBytesValue = MachineKey.Unprotect(Convert.FromBase64String(Uri.UnescapeDataString(encryptedValue)), EncryptionReason);
                if (decryptedBytesValue != null)
                {
                    var decryptedValue = Encoding.Default.GetString(decryptedBytesValue);
                    var user = decryptedValue.Split(';')[0];
                    var date = DateTime.Parse(decryptedValue.Split(';')[1]);
                    if (ValidDateTimeSessionCookie(date))
                    {
                        context.User = new SimplePrincipal(new SimpleIdentity(user, "MSA"));
                    }
                }
            }
            catch (Exception e)
            {
                // we need to authenticate
                //TODO: log the error 
                Trace.TraceError(e.ToString());
                context.Response.Redirect(WindowsLiveLogin.GetLoginUrl(), endResponse:true);
            }
        }

        private static bool ValidDateTimeSessionCookie(DateTime date)
        {
            return date < DateTime.UtcNow.AddHours(SessionCookieHoursValid);
        }

        public static HttpResponseMessage HandleLogin(HttpContext context)
        {
            var user = WindowsLiveLogin.ProcessLogin(context.Request.Form);
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            if (user != null)
            {
                var value = string.Format("{0};{1}", user.Id, DateTime.UtcNow);
                var encryptedBytesValue = MachineKey.Protect(Encoding.Default.GetBytes(value), EncryptionReason);
                var encryptedValue = Convert.ToBase64String(encryptedBytesValue);
                response.Headers.AddCookies(new[]
                {
                    new CookieHeaderValue(LoginSessionCookie, encryptedValue){Path = "/"}
                });
            }
            response.Headers.Location = new Uri("/", UriKind.Relative);
            return response;
        }
    }
}