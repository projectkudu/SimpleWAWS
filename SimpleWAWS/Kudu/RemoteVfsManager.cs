using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kudu.Client.Infrastructure;
using SimpleWAWS.Code;

namespace Kudu.Client.Editor
{
    public class RemoteVfsManager : KuduRemoteClientBase
    {
        private int _retryCount;
        public RemoteVfsManager(string serviceUrl, ICredentials credentials = null, HttpMessageHandler handler = null, int retryCount = 0)
            : base(serviceUrl, credentials, handler)
        {
            _retryCount = retryCount;
        }

        public Task Delete(string path, bool recursive = false)
        {
            return RetryHelper.Retry(() =>
            {
                using (var request = new HttpRequestMessage())
                {
                    path += recursive ? "?recursive=true" : String.Empty;

                    request.Method = HttpMethod.Delete;
                    request.RequestUri = new Uri(path, UriKind.Relative);
                    request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);

                    return Client.SendAsync(request);
                }
            }, _retryCount);
        }

        public Task Put(string remotePath, string localPath)
        {
            return RetryHelper.Retry(() =>
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Put;
                    request.RequestUri = new Uri(remotePath, UriKind.Relative);
                    request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
                    request.Content = new StreamContent(new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read));
                    return Client.SendAsync(request);
                }
            }, _retryCount);
        }
    }
}

