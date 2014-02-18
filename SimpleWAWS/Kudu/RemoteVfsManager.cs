using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Kudu.Client.Infrastructure;

namespace Kudu.Client.Editor
{
    public class RemoteVfsManager : KuduRemoteClientBase
    {
        public RemoteVfsManager(string serviceUrl, ICredentials credentials = null, HttpMessageHandler handler = null)
            : base(serviceUrl, credentials, handler)
        {
        }

        public async Task Delete(string path, bool recursive = false)
        {
            using (var request = new HttpRequestMessage())
            {
                path += recursive ? "?recursive=true" : String.Empty;

                request.Method = HttpMethod.Delete;
                request.RequestUri = new Uri(path, UriKind.Relative);
                request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);

                await Client.SendAsync(request);
            }
        }
    }
}

