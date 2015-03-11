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

namespace SimpleWAWS.Code
{
    public static class MobileHelper
    {

        public static PushStreamContent CreateClientZip(MobileClientPlatform platform, Dictionary<string, string> replacements)
        {
            if (platform == MobileClientPlatform.Windows)
            {
                return CreateZip("Windows_Universal.zip", zip =>
                {
                    var rootPath = HostingEnvironment.MapPath("~/App_Data/MobileClientApp/Windows");
                    foreach (var fileName in Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories))
                    {
                        if (fileName.EndsWith("App.xaml.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            var content = File.ReadAllText(fileName);

                            foreach (var pair in replacements)
                                content = content.Replace(pair.Key, pair.Value);

                            using (var contentStream = content.AsStream())
                            {
                                zip.AddFile(fileName, rootPath, contentStream);
                            }
                        }
                        else
                        {
                            zip.AddFile(fileName, rootPath);
                        }
                    }
                });
            }
            else
            {
                throw new Exception(string.Format("{0} is an unsuppoerted Platform", platform));
            }
        }

        private static PushStreamContent CreateZip(string fileName, Action<ZipArchive> onZip)
        {
            var content = new PushStreamContent((outputStream, httpContent, transportContext) =>
            {
                try
                {
                    using (var zip = new ZipArchive(new StreamWrapper(outputStream), ZipArchiveMode.Create, leaveOpen: false))
                    {
                        onZip(zip);
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }
            });
            content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            content.Headers.ContentDisposition.FileName = fileName;
            return content;
        }
    }
}