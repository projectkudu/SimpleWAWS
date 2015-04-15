using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<JObject> GenerateCsmTemplate(this ApiApp apiApp)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Post, CsmTemplates.AppServiceGenerateCsmDeployTemplate.Bind(apiApp), apiApp.GeneratePayload());
            csmResponse.EnsureSuccessStatusCode();
            var response = await csmResponse.Content.ReadAsAsync<JObject>();
            return (JObject) response["value"];
        }
    }
}