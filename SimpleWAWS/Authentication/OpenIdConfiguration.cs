using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;

namespace SimpleWAWS.Authentication
{
    public static class OpenIdConfiguration
    {
        private static readonly Dictionary<string, IEnumerable<SecurityToken>> SigningKeyMap = new Dictionary<string, IEnumerable<SecurityToken>>();

        public static IEnumerable<SecurityToken> GetIssuerSigningKeys(string jwt)
        {
            var signingKey = GetSigningKeyFromJWT(jwt);
            if (SigningKeyMap.ContainsKey(signingKey))
            {
                return SigningKeyMap[signingKey];
            }
            UpdateKeysMap();
            if (!SigningKeyMap.ContainsKey(signingKey))
            {
                throw new Exception("Unknown singing cert from issuer");
            }
            return SigningKeyMap[signingKey];
        }

        private static void UpdateKeysMap()
        {
            var aadIssuerKeys = AuthSettings.AADIssuerKeys;
            var googleIssuerCerts = AuthSettings.GoogleIssuerCerts;
            var content = GetContentFromUrl(aadIssuerKeys);
            var aadKeys = JsonConvert.DeserializeObject<JWTSingingKeys>(content);
            foreach (var jwtSingingKey in aadKeys.Keys)
            {
                SigningKeyMap[jwtSingingKey.Thumbprint] = jwtSingingKey.GetSecurityTokens();
            }

            content = GetContentFromUrl(googleIssuerCerts);
            var googleCerts = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            foreach (var certPair in googleCerts)
            {
                SigningKeyMap[certPair.Key] = GetCertFromOpenSSLCert(certPair.Value);
            }
        }

        private static string GetContentFromUrl(string url)
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


        private static string GetSigningKeyFromJWT(string jwt)
        {
            var encodedSingingInfo = jwt.Substring(0, jwt.IndexOf('.'));
            var singingInfoString = Encoding.UTF8.GetString(Convert.FromBase64String(encodedSingingInfo.PadBase64()));
            var signingInfo = JsonConvert.DeserializeObject<JWTSingingInfo>(singingInfoString);
            return signingInfo.Thumbprint ?? signingInfo.KeyId;
        }

        private static IEnumerable<SecurityToken> GetCertFromOpenSSLCert(string rawCert)
        {
            var certBytes = Encoding.UTF8.GetBytes(rawCert.Replace("\\n", "\n"));
            yield return new X509SecurityToken(new X509Certificate2(certBytes));
        }

        private class JWTSingingInfo
        {
            [JsonProperty("type")]
            public string Type { get; set; }
            [JsonProperty("alg")]
            public string Algorithm { get; set; }
            [JsonProperty("x5t")]
            public string Thumbprint { get; set; }
            [JsonProperty("kid")]
            public string KeyId { get; set; }
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
                    yield return new X509SecurityToken(new X509Certificate2(Convert.FromBase64String(rawCert.PadBase64())));
                }
            }
        }
    }
}