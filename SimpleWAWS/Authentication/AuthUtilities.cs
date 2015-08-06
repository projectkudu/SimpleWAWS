using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

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