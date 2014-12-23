using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using Microsoft.WindowsAzure.Management.WebSites.Models;

namespace SimpleWAWS.Code
{
    public static class Extensions
    {
        private const string DefaultEncryptReason = "DefaultEncryptReason";
        public static string Serialize(this WebSiteGetPublishProfileResponse.PublishProfile profile)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("<publishProfile ");
            var type = profile.GetType();
            foreach (var property in type.GetProperties())
            {
                if (property.Name.Equals("databases", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                stringBuilder.AppendFormat("{0}=\"{1}\" ", SerializeFixups(property.Name.FirstCharToLower()), property.GetValue(profile));
            }
            stringBuilder.Append("></publishProfile>");
            return stringBuilder.ToString();
        }

        public static string SerializeFixups(string value)
        {
            switch (value.ToLowerInvariant())
            {
                case "userpassword":
                    return "userPWD";
                case "msdeploysite":
                    return "msdeploySite";
                case "sqlserverdbconnectionstring":
                    return "SQLServerDBConnectionString";
                case "destinationappuri":
                    return "destinationAppUrl";
                default:
                    return value;
            }
        }

        public static string FirstCharToLower(this string str)
        {
            return Char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static Stream ToStream(this string str)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string Encrypt(this string str, string reason = null)
        {
            var valueBytes = Encoding.Default.GetBytes(str);
            var encryptedBytes = MachineKey.Protect(valueBytes, reason ?? DefaultEncryptReason);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(this string str, string reason = null)
        {
            var encryptesBytes = Convert.FromBase64String(str);
            var decryptedBytes = MachineKey.Unprotect(encryptesBytes, reason ?? DefaultEncryptReason);
            if (decryptedBytes != null)
            {
                return Encoding.Default.GetString(decryptedBytes);
            }
            throw new Exception("decrypted value is null");
        }

        public static bool IsAjaxRequest(this HttpContext context)
        {
            return context.Request.Headers["X-Requested-With"] != null &&
                   context.Request.Headers["X-Requested-With"].Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}