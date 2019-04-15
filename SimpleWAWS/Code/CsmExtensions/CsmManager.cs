using ARMClient.Library;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        private static readonly AzureClient csmClient;
        private static IEnumerable<string> _subscriptions;
        static CsmManager()
        {
            csmClient = new AzureClient(retryCount: 3);
            csmClient.ConfigureSpnLogin(SimpleSettings.TrySPTenantId, SimpleSettings.TrySPUserName, SimpleSettings.TrySPPassword);
        }

        public static async Task<IEnumerable<string>> GetSubscriptions()
        {
            if (_subscriptions == null)
            {
                // Load all subscriptions
                var csmSubscriptions = await CsmManager.GetSubscriptionNamesToIdMap();
                _subscriptions = csmSubscriptions
                    .Select(sn =>
                    {
                        return csmSubscriptions[sn.Key];
                    });
            }
            return _subscriptions;
        }


        static AzureClient GetClient(SubscriptionType subscriptionType)
        {
         return csmClient;
        }


        public static async Task<Dictionary<string, string>> GetSubscriptionNamesToIdMap()
        {
            var response = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.Subscriptions.Bind(""));
            await response.EnsureSuccessStatusCodeWithFullError();

            var appServiceSubscriptions = await response.Content.ReadAsAsync<CsmSubscriptionsArray>();


            return (appServiceSubscriptions.value)
                   .GroupBy(sub => sub.subscriptionId)
                   .Select(group => group.First()).ToDictionary(k => k.displayName, v => v.subscriptionId);
        }

        //private static IEnumerable<IEnumerable<T>> BatchEnumerable<T>(this IEnumerable<T> source, int batchSize)
        //{
        //    var batch = new List<T>(batchSize);
        //    foreach (var item in source)
        //    {
        //        batch.Add(item);
        //        if (batch.Count == batchSize)
        //        {
        //            yield return batch;
        //            batch = new List<T>(batchSize);
        //        }
        //    }

        //    if (batch.Any())
        //    {
        //        yield return batch;
        //    }
        //}
        //public static async Task<JObject> GetLoadedResources()
        //{

        //    var tryAppServiceResponse = await GetGraphClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.LoadedResources);
        //    await tryAppServiceResponse.EnsureSuccessStatusCodeWithFullError();
        //    return await tryAppServiceResponse.Content.ReadAsAsync<JObject>();
        //}
    }

}
