using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Configuration;
using Newtonsoft.Json;

namespace SimpleWAWS.Authentication
{
    public static class OpenIdConfiguration
    {
        private static readonly Dictionary<string, IEnumerable<SecurityToken>> ThumbprintKeyMap = new Dictionary<string, IEnumerable<SecurityToken>>();

        public static IEnumerable<SecurityToken> GetIssuerSigningKeys(string jwt)
        {
            var thumbprint = GetX5TFromJWT(jwt);
            if (ThumbprintKeyMap.ContainsKey(thumbprint))
            {
                return ThumbprintKeyMap[thumbprint];
            }
            UpdateKeysMap();
            if (!ThumbprintKeyMap.ContainsKey(thumbprint))
            {
                throw new Exception("Unknown singing cert from issuer");
            }
            return ThumbprintKeyMap[thumbprint];
        }

        private static void UpdateKeysMap()
        {
            var request = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings[Constants.AADIssuerKeys]);
            JWTSingingKeys keys = null;
            using (var response = request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    keys = JsonConvert.DeserializeObject<JWTSingingKeys>(reader.ReadToEnd());
                }
            }
            foreach (var jwtSingingKey in keys.Keys)
            {
                ThumbprintKeyMap[jwtSingingKey.Thumbprint] = jwtSingingKey.GetSecurityTokens();
            }
        }


        public static string GetX5TFromJWT(string jwt)
        {
            var encodedSingingInfo = jwt.Substring(0, jwt.IndexOf('.'));
            var singingInfoString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedSingingInfo));
            var singingInfo = JsonConvert.DeserializeObject<JWTSingingInfo>(singingInfoString);
            return singingInfo.Thumbprint;
        }

        private class JWTSingingInfo
        {
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("alg")]
            public string Algorithm { get; set; }
            [JsonProperty("x5t")]
            public string Thumbprint { get; set; }
        }

        private class JWTSingingKeys
        {
            [JsonProperty("keys")]
            public JWTSingingKey[] Keys { get; set; }
        }

        private class JWTSingingKey
        {
            [JsonProperty("x5t")]
            public string Thumbprint { get; set; }
            [JsonProperty("x5c")]
            public string[] CertificateRawData { get; set; }

            public IEnumerable<SecurityToken> GetSecurityTokens()
            {
                foreach (var rawCert in this.CertificateRawData)
                {
                    yield return new X509SecurityToken(new X509Certificate2(Convert.FromBase64String(rawCert)));
                }
            }
        }
    }
}