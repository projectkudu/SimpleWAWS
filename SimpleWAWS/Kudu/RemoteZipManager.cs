using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kudu.Client.Infrastructure;
using SimpleWAWS.Code;

namespace Kudu.Client.Zip
{
    public class RemoteZipManager : KuduRemoteClientBase
    {
        private int _retryCount;
        public RemoteZipManager(string serviceUrl, ICredentials credentials = null, HttpMessageHandler handler = null, int retryCount = 0)
            : base(serviceUrl, credentials, handler)
        {
            _retryCount = retryCount;
        }

        private async Task PutZipStreamAsync(string path, Stream zipFile)
        {
            await RetryHelper.Retry(async () =>
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Put;
                    request.RequestUri = new Uri(path, UriKind.Relative);
                    request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
                    request.Content = new StreamContent(zipFile);
                    await Client.SendAsync(request);
                }
            }, _retryCount);
        }

        public async Task PutZipFileAsync(string path, string localZipPath)
        {
            using (var stream = File.OpenRead(localZipPath))
            {
                await PutZipStreamAsync(path, stream);
            }
        }

        public async Task<Stream> GetZipFileStreamAsync(string path)
        {
            var response = await Client.GetAsync(new Uri(path, UriKind.Relative), HttpCompletionOption.ResponseHeadersRead);
            return await response.Content.ReadAsStreamAsync();
        }
    }
}

