using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<JToken> Deploy(this CsmDeployment csmDeployment, bool block = false)
        {
            var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put, CsmTemplates.CsmTemplateDeployment.Bind(csmDeployment), csmDeployment.CsmTemplate);
            csmResponse.EnsureSuccessStatusCode();
            var content = await csmResponse.Content.ReadAsAsync<JToken>();
            if (!block) return content;

            Func<JToken, string> getProvisioningState = (jtoken) =>
            {
                return jtoken["properties"] != null && jtoken["properties"]["provisioningState"] != null
                ? jtoken["properties"]["provisioningState"].ToString()
                : string.Empty;
            };

            var result = string.Empty;

            do
            {
                result = getProvisioningState(content);

                if (string.IsNullOrEmpty(result))
                {
                    throw new Exception(string.Format("Response doesn't have properties or provisioningState: {0}", content.ToString(Newtonsoft.Json.Formatting.None)));
                }
                else if (result.Equals("Accepted") || result.Equals("Running"))
                {
                    await Task.Delay(1000);
                }
                else if (result.Equals("Succeeded"))
                {
                    return content;
                }
                else if (result.Equals("Failed"))
                {
                    throw new Exception(string.Format("Deploying CSM template failed, ID: {0}", content["id"]));
                }
                else
                {
                    SimpleTrace.Diagnostics.Error("Unknown status code from CSM {provisioningState}", result);
                    throw new Exception("Unknown status " + result);
                }

                csmResponse = await csmClient.HttpInvoke(HttpMethod.Get, CsmTemplates.CsmTemplateDeployment.Bind(csmDeployment));
                csmResponse.EnsureSuccessStatusCode();
                content = await csmResponse.Content.ReadAsAsync<JToken>();
            } while (block);

            return content;
        }
    }
}