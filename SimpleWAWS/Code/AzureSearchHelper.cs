using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class AzureSearchHelper
    {
        private static string[] _apiKeys;
        private static Random _random;
        static AzureSearchHelper()
        {
            if (!string.IsNullOrEmpty(SimpleSettings.SearchServiceApiKeys))
                _apiKeys = SimpleSettings.SearchServiceApiKeys.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            else
                _apiKeys = Enumerable.Empty<string>().ToArray();

            _random = new Random();
        }

        public static string GetApiKey()
        {
            if (_apiKeys.Length == 0) return string.Empty;
            return _apiKeys[_random.Next(0, _apiKeys.Length)];
        }
    }
}