using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Kudu.Client.Infrastructure;

namespace SimpleWAWS.Kudu
{
    public class ProcessManager : KuduRemoteClientBase
    {
        public ProcessManager(string serviceUrl, ICredentials credentials = null, HttpMessageHandler handler = null)
            : base(serviceUrl, credentials, handler)
        {
        }

        public async Task Kill(int processId = 0)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Delete;
                request.RequestUri = new Uri("api/diagnostics/processes/0", UriKind.Relative);
                request.Headers.IfMatch.Add(EntityTagHeaderValue.Any);
                await Client.SendAsync(request);
            }
        }
    }
}