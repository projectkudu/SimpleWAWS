using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kudu.Client.Infrastructure;
using SimpleWAWS.Code;
using SimpleWAWS;
using SimpleWAWS.Trace;

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
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Put;
                request.RequestUri = new Uri(path, UriKind.Relative);
                request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
                request.Content = new StreamContent(zipFile);
                var response = await Client.SendAsync(request);
                await response.EnsureSuccessStatusCodeWithFullError();
            }
        }
        private async Task<Uri> PostZipStreamAsync(string path, Stream zipFile)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(path, UriKind.Relative);
                request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
                request.Content = new StreamContent(zipFile);
                var response = await Client.SendAsync(request);
                await response.EnsureSuccessStatusCodeWithFullError();
                return response.Headers.Location;
            }
        }
        public async Task PutZipFileAsync(string path, string zipUrl)
        {
            await RetryHelper.Retry(async () =>
            {
                var trial = 0;
                SimpleTrace.TraceInformation($"Site Zip PUT started trial ({++trial}/{_retryCount}): for {zipUrl}->{ServiceUrl}");
                using (var stream = await GetHttpStream(zipUrl))
                {
                    await PutZipStreamAsync(path, stream);
                }
            }, _retryCount);
        }
        public async Task<Uri> PostZipFileAsync(string path, string zipUrl)
        {
            return await RetryHelper.Retry(async () =>
            {
                var trial = 0;
                SimpleTrace.TraceInformation($"Site POST Zip Deploy started trial ({++trial}/{_retryCount}): for {zipUrl}->{ServiceUrl}");
                using (var stream = await GetHttpStream(zipUrl))
                {
                    return await PostZipStreamAsync(path, stream);
                 
                }
            }, _retryCount);
        }
        private async Task<Stream> GetHttpStream(string zipUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
        }
        public async Task<Stream> GetZipFileStreamAsync(string path)
        {
            var response = await Client.GetAsync(new Uri(path, UriKind.Relative), HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
    }
}