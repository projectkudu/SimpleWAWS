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
        public static async Task<JToken> Deploy(this CsmDeployment csmDeployment)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.DeployCsmTemplate.Bind(csmDeployment), csmDeployment.CsmTemplate);
            csmResponse.EnsureSuccessStatusCode();
            return await csmResponse.Content.ReadAsAsync<JToken>();
        }
    }
}