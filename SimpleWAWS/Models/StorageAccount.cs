using SimpleWAWS.Code;
using System;
using System.Globalization;

namespace SimpleWAWS.Models
{
    public class StorageAccount : BaseResource
    {
        private string _csmIdTemplate = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Storage/storageAccounts/{2}";

        public string StorageAccountName { get; private set; }

        public override string CsmId
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, this._csmIdTemplate, this.SubscriptionId, this.ResourceGroupName, this.StorageAccountName);
            }
        }

        public string StorageAccountKey { get; set; }
        public bool IsFunctionsStorageAccount
        {
            get
            {
                return !string.IsNullOrEmpty(StorageAccountName) &&
                    StorageAccountName.StartsWith(Constants.FunctionsStorageAccountPrefix, StringComparison.OrdinalIgnoreCase);
            }
        }

        public StorageAccount(string subscriptionId, string resourceGroupName, string storageAccountName)
            : base(subscriptionId, resourceGroupName)
        {
            this.StorageAccountName = storageAccountName;
        }
    }
}