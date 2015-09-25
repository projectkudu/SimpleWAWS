using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
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
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.AppServiceGenerateCsmDeployTemplate.Bind(apiApp), apiApp.GeneratePayload());
            csmResponse.EnsureSuccessStatusCode();
            var response = await csmResponse.Content.ReadAsAsync<JObject>();
            return (JObject) response["value"];
        }

        public static async Task SetAccessLevel(this ApiApp apiApp, string accessLevel)
        {
            var csmResponse = await csmClient.HttpInvoke("PATCH", ArmUriTemplates.ApiApp.Bind(apiApp), new CsmWrapper<CsmApiApp> { properties = new CsmApiApp { accessLevel = accessLevel } });
        }
    }
}