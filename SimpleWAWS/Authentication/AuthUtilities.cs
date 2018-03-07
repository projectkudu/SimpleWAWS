using System;
using System.IO;
using System.Net;

namespace SimpleWAWS.Authentication
{
    public static class AuthUtilities
    {
        public static string GetContentFromUrl(string url)
        {
            //specify to use TLS 1.2 as default connection
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var request = (HttpWebRequest)WebRequest.Create(url);
            using (var response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        public static string GetContentFromGitHubUrl(string url, string method = "GET", bool jsonAccept = false, bool addGitHubHeaders = false, string AuthorizationHeader = "")
        {

            //specify to use TLS 1.2 as default connection
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.UserAgent = "x-ms-try-appservice";
            if (jsonAccept)
            {
                request.Accept = "application/json";
            }
            if (addGitHubHeaders)
            {
                request.Accept = "application/vnd.github.v3+json";
            }
            if (!String.IsNullOrEmpty(AuthorizationHeader))
            {
                request.Headers.Add("Authorization", AuthorizationHeader);
            }
            using (var response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}