using Kudu.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SimpleWAWS.Models
{
    public static class MobileHelper
    {
        public static PushStreamContent CreateClientZip(MobileClientPlatform platform, string templateName, Dictionary<string, string> replacements)
        {
            var clientPath = HostingEnvironment.MapPath(string.Format(CultureInfo.InvariantCulture, "~/App_Data/MobileClientApp/{0}/{1}", templateName, platform.ToString()));
            return CreateZip(string.Format(CultureInfo.InvariantCulture, "{0}.zip", platform.ToString()), zip =>
            {
                foreach (var fileName in Directory.GetFiles(clientPath, "*", SearchOption.AllDirectories))
                {
                    var replacedFileName = fileName;

                    if (string.IsNullOrEmpty(Path.GetExtension(fileName)) ||
                        new[] { ".png", ".jpg", ".keystore", ".exe", ".dll", ".sketch" }
                        .Any(ext => ext.Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase)))
                    {
                        foreach (var pair in replacements)
                        {
                            replacedFileName = replacedFileName.Replace(pair.Key, pair.Value);
                        }

                        zip.AddFile(fileName, replacedFileName, clientPath);
                    }
                    else
                    {
                        var fileEncoding = GetEncoding(fileName);
                        var content = File.ReadAllText(fileName, fileEncoding);
                        foreach (var pair in replacements)
                        {
                            content = content.Replace(pair.Key, pair.Value);
                            replacedFileName = replacedFileName.Replace(pair.Key, pair.Value);
                        }

                        using (var contentStream = content.AsStream(fileEncoding))
                        {
                            zip.AddFile(replacedFileName, clientPath, contentStream);
                        }
                    }
                }
            });
       }

        /// <summary>
        /// http://stackoverflow.com/a/19283954
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
        private static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.ASCII;
        }

        private static PushStreamContent CreateZip(string fileName, Action<ZipArchive> onZip)
        {
            var content = new PushStreamContent((outputStream, httpContent, transportContext) =>
            {
                using (var zip = new ZipArchive(new StreamWrapper(outputStream), ZipArchiveMode.Create, leaveOpen: false))
                {
                    onZip(zip);
                }
            });
            content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            content.Headers.ContentDisposition.FileName = fileName;
            return content;
        }
    }
}