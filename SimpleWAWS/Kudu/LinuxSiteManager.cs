using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kudu.Client.Infrastructure;
using SimpleWAWS.Code;
using SimpleWAWS;

namespace LinuxSiteManager.Client
{
    public class LinuxSiteManager : HttpClient
    {
        private int _retryCount;
        private HttpClient httpClient;
        public LinuxSiteManager(int retryCount = 0)
            : base()
        {
            _retryCount = retryCount;
            httpClient  = new HttpClient();
        }

        private async Task CheckSiteDeploymentStatus(string path)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(path, UriKind.Absolute);
                var response = await httpClient.SendAsync(request);
                await response.EnsureSuccessStatusCodeAndNewSiteContentWithFullError();
            }
        }

        public async Task CheckSiteDeploymentStatusAsync(string path)
        {
            await RetryHelper.Retry(async () =>
            {
                {
                    await CheckSiteDeploymentStatus(path);
                }
            }, _retryCount, delay:5000);
        }

    }
}

