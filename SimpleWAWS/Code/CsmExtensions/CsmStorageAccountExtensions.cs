using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<StorageAccount> Load(this StorageAccount storageAccount, CsmWrapper<CsmStorageAccount> csmStorageAccount = null)
        {
            Validate.ValidateCsmStorageAccount(storageAccount);
            if (!storageAccount.IsFunctionsStorageAccount) return storageAccount;

            if (csmStorageAccount?.properties?.provisioningState != "Succeeded")
            {
                csmStorageAccount = await WaitUntilReady(storageAccount);
            }

            var csmStorageResponse = await csmClient.HttpInvoke(HttpMethod.Post, ArmUriTemplates.StorageListKeys.Bind(storageAccount));
            await csmStorageResponse.EnsureSuccessStatusCodeWithFullError();

            var keys = await csmStorageResponse.Content.ReadAsAsync<Dictionary<string, string>>();
            storageAccount.StorageAccountKey = keys.Select(s => s.Value).FirstOrDefault();

            return storageAccount;
        }

        public static async Task<CsmWrapper<CsmStorageAccount>> WaitUntilReady(this StorageAccount storageAccount)
        {
            Validate.ValidateCsmStorageAccount(storageAccount);
            var isSucceeded = false;
            var tries = 40;
            CsmWrapper<CsmStorageAccount> csmStorageAccount = null;
            do
            {
                var csmStorageResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.StorageAccount.Bind(storageAccount));
                await csmStorageResponse.EnsureSuccessStatusCodeWithFullError();
                csmStorageAccount = await csmStorageResponse.Content.ReadAsAsync<CsmWrapper<CsmStorageAccount>>();
                isSucceeded = csmStorageAccount.properties.provisioningState.Equals("Succeeded", StringComparison.OrdinalIgnoreCase);
                tries--;
                if (!isSucceeded) await Task.Delay(500);
            } while (!isSucceeded && tries > 0);

            if (!isSucceeded) throw new StorageNotReadyException();

            return csmStorageAccount;
        }
    }
}