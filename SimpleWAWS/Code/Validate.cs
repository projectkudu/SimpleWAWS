using SimpleWAWS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class Validate
    {
        public static void NotNullOrEmpty(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, "{0} cannot be null or empty. Current value is: '{1}'", fieldName, value == null ? "null" : value));
            }
        }

        public static void NotNull(object obj, string fieldName)
        {
            if (obj == null)
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, "{0} cannot be null", fieldName));
            }
        }

        public static void ValidateCsmSite(Site site)
        {
            NotNull(site, "site");
            NotNullOrEmpty(site.SubscriptionId, "SubscriptionId");
            NotNullOrEmpty(site.ResourceGroupName, "resourceGroupName");
            NotNullOrEmpty(site.SiteName, "Name");
        }

        public static void ValidateCsmServerFarm(ServerFarm serverFarm)
        {
            NotNull(serverFarm, "serverFarm");
            NotNullOrEmpty(serverFarm.Location, "Location");
            NotNullOrEmpty(serverFarm.ServerFarmName, "serverFarmName");
            NotNull(serverFarm.Sku, "Sku");
        }
        public static void ValidateCsmResourceGroup(ResourceGroup resourceGroup)
        {

        }

        internal static void ValidateCsmSubscription(Subscription subscription)
        {
            NotNull(subscription, "subscription");
            NotNullOrEmpty(subscription.SubscriptionId, "subscriptionId");
        }

        internal static void ValidateCsmStorageAccount(StorageAccount storageAccount)
        {
            NotNull(storageAccount, nameof(storageAccount));
            NotNullOrEmpty(storageAccount.SubscriptionId, nameof(storageAccount.SubscriptionId));
            NotNullOrEmpty(storageAccount.ResourceGroupName, nameof(storageAccount.ResourceGroupName));
            NotNullOrEmpty(storageAccount.StorageAccountName, nameof(storageAccount.StorageAccountName));
        }
    }
}