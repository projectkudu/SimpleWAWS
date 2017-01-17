using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Kudu.Client.Infrastructure
{
    public static class HttpClientHelper
    {
        private static readonly char[] uriPathSeparator = new char[] { '/' };

        public static HttpClient CreateClient(string serviceUrl, ICredentials credentials = null, HttpMessageHandler handler = null)
        {
            if (serviceUrl == null)
            {
                throw new ArgumentNullException("serviceUrl");
            }
            else
            {
                SimpleWAWS.Trace.SimpleTrace.Diagnostics.Information(serviceUrl);
            }

            HttpMessageHandler effectiveHandler = handler ?? CreateClientHandler(serviceUrl, credentials);
            Uri serviceAddr = new Uri(serviceUrl);
            HttpClient client = new HttpClient(effectiveHandler)
            {
                BaseAddress = serviceAddr,
                MaxResponseContentBufferSize = 30 * 1024 * 1024
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public static HttpClientHandler CreateClientHandler(string serviceUrl, ICredentials credentials)
        {
            if (serviceUrl == null)
            {
                throw new ArgumentNullException("serviceUrl");
            }

            // Set up our own HttpClientHandler and configure it
            HttpClientHandler clientHandler = new HttpClientHandler();

            if (credentials != null)
            {
                // Set up credentials cache which will handle basic authentication
                CredentialCache credentialCache = new CredentialCache();

                // Get base address without terminating slash
                string credentialAddress = new Uri(serviceUrl).GetLeftPart(UriPartial.Authority).TrimEnd(uriPathSeparator);

                // Add credentials to cache and associate with handler
                NetworkCredential networkCredentials = credentials.GetCredential(new Uri(credentialAddress), "Basic");
                credentialCache.Add(new Uri(credentialAddress), "Basic", networkCredentials);
                clientHandler.Credentials = credentialCache;
                clientHandler.PreAuthenticate = true;
            }

            // Our handler is ready
            return clientHandler;
        }
    }
}
