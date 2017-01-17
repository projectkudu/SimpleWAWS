﻿using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class GlobalizationManager
    {
        private const string _cultureCookieName = "culture";
        private static readonly char[] _splitOn = new[] { '/', '?' };
        public static void SetCurrentCulture(HttpContextWrapper context)
        {
            CultureInfo culture;
            if (TryGetCulture(context, out culture))
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                var cultureCookie = context.Request.Cookies[_cultureCookieName];
                if (cultureCookie == null ||
                    !cultureCookie.Value.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Cookies.Add(new HttpCookie(_cultureCookieName, culture.Name) { Path = "/", Expires = DateTime.UtcNow.AddYears(2) });
                }
            }
        }

        public static bool TryGetCulture(HttpContextWrapper context, out CultureInfo culture)
        {
            culture = null;
            try
            {
                var cultureString = context.Request.RawUrl.Split(_splitOn, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(cultureString) &&
                    TryParseCulture(cultureString, out culture))
                {
                    return true;
                }
                else if (context.Request.Cookies[_cultureCookieName] != null &&
                    TryParseCulture(context.Request.Cookies[_cultureCookieName].Value, out culture))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseCulture(string cultureString, out CultureInfo culture)
        {
            culture = null;
            try
            {
                if (cultureString.IndexOf("-", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    return false;
                }

                culture = new CultureInfo(cultureString);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}