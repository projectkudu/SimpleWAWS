using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using System.IO.Compression;
using System.Threading.Tasks;
using SimpleWAWS.Models;

namespace SimpleWAWS
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

        public static string Encrypt(this string str, string reason = null)
        {
            var valueBytes = Encoding.Default.GetBytes(str);
            var encryptedBytes = MachineKey.Protect(valueBytes, reason ?? DefaultEncryptReason);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string Decrypt(this string str, string reason = null)
        {
            var encryptesBytes = Convert.FromBase64String(str.PadBase64());
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

        public static bool IsBrowserRequest(this HttpContext context)
        {
            return context.Request.UserAgent != null
                && (context.Request.UserAgent.StartsWith("Mozilla/") || context.Request.UserAgent.StartsWith("Opera/"));
        }

        public static string PadBase64(this string value)
        {
            return value.Length % 4 == 0
                ? value
                : value.PadRight(value.Length + (4 - value.Length % 4), '=');
        }

        public static string RemoveNewLines(this string value)
        {
            return value.Replace("\r\n", "_").Replace('\n', '_');
        }

        public static void AddFile(this ZipArchive archive, string fileName, string zippedName, string zipRoot)
        {
            using (var stream = File.Open(fileName, FileMode.Open))
            {
                archive.AddFile(zippedName, zipRoot, stream);
            }
        }

        public static void AddFile(this ZipArchive archive, string fileName, string zipRoot, Stream contentStream)
        {
            var entry = archive.CreateEntry(fileName.FixFileNameForZip(zipRoot), CompressionLevel.Fastest);
            using (var zipStream = entry.Open())
            {
                contentStream.CopyTo(zipStream);
            }
        }

        public static string FixFileNameForZip(this string value, string zipRoot)
        {
            return value.Substring(zipRoot.Length).TrimStart(new[] { '\\' }).Replace('\\', '/');
        }

        public static Stream AsStream(this string value)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write(value);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static IEnumerable<T> NotDefaults<T>(this IEnumerable<T> collection)
        {
            return collection.Where(e => !EqualityComparer<T>.Default.Equals(e, default(T)));
        }
    }
}
