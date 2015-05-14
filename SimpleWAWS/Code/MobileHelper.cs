using Kudu.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SimpleWAWS.Models
{
    public static class MobileHelper
    {
        public static PushStreamContent CreateClientZip(MobileClientPlatform platform, Dictionary<string, string> replacements)
        {
            var clientPath = HostingEnvironment.MapPath(string.Format("~/App_Data/MobileClientApp/{0}", platform.ToString()));
            return CreateZip(string.Format("{0}.zip", platform.ToString()), zip =>
            {
                foreach (var fileName in Directory.GetFiles(clientPath, "*", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(fileName);
                    var replacedFileName = fileName;

                    if (string.IsNullOrEmpty(Path.GetExtension(fileName)) || Path.GetExtension(fileName) == ".png")
                    {
                        foreach (var pair in replacements)
                        {
                            replacedFileName = replacedFileName.Replace(pair.Key, pair.Value);
                        }

                        zip.AddFile(fileName, replacedFileName, clientPath);
                    }
                    else
                    {
                        foreach (var pair in replacements)
                        {
                            content = content.Replace(pair.Key, pair.Value);
                            replacedFileName = replacedFileName.Replace(pair.Key, pair.Value);
                        }

                        using (var contentStream = content.AsStream())
                        {
                            zip.AddFile(replacedFileName, clientPath, contentStream);
                        }
                    }
                }
            });
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