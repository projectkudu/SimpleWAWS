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
        public static async Task<StorageAccount> Load(this StorageAccount storageAccount, CsmWrapper<CsmStorageAccount> csmStorageAccount = null)
        {
            Validate.ValidateCsmStorageAccount(storageAccount);
            if (!storageAccount.IsFunctionsStorageAccount) return storageAccount;

            var csmStorageResponse = await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.StorageListKeys.Bind(storageAccount));
            await csmStorageResponse.EnsureSuccessStatusCodeWithFullError();

            var keys = await csmStorageResponse.Content.ReadAsAsync<Dictionary<string, string>>();
            storageAccount.StorageAccountKey = keys.Select(s => s.Value).FirstOrDefault();

            return storageAccount;
        }
    }
}