using System.IO;
using System.Net;

namespace SimpleWAWS.Authentication
{
    public static class AuthUtilities
    {
        public static string GetContentFromUrl(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
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