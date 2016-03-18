using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System.Configuration;
using SimpleWAWS.Code;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;

namespace SimpleWAWS.Models
{
    public static class StorageHelper
    {
        private static readonly CloudStorageAccount StorageAccount;
        private static readonly CloudBlobClient BlobClient;
        private static readonly CloudQueueClient QueueClient;
        private static readonly MetricsLevel DefaultMetricsLevel = MetricsLevel.ServiceAndApi;
        private const int DefaultRetentionDays = 10;


        static StorageHelper()
        {
            if (!string.IsNullOrEmpty(SimpleSettings.StorageConnectionString))
            {
                StorageAccount = CloudStorageAccount.Parse(SimpleSettings.StorageConnectionString);
                BlobClient = StorageAccount.CreateCloudBlobClient();
                QueueClient = StorageAccount.CreateCloudQueueClient();
            }
        }

        public static async Task UploadBlob(string blobName, Stream contentStream)
        {
            if (!string.IsNullOrEmpty(SimpleSettings.FreeSitesIISLogsBlob))
            {
                var container = BlobClient.GetContainerReference(SimpleSettings.FreeSitesIISLogsBlob);
                await container.CreateIfNotExistsAsync();
                var blob = container.GetBlockBlobReference(blobName);
                await blob.UploadFromStreamAsync(contentStream);
            }
        }

        public static async Task AddQueueMessage(object message)
        {
            if (!string.IsNullOrEmpty(SimpleSettings.FreeSitesIISLogsQueue))
            {
                var queue = QueueClient.GetQueueReference(SimpleSettings.FreeSitesIISLogsQueue);
                await queue.CreateIfNotExistsAsync();
                await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(message)));
            }
        }

        public static Task EnableStorageAnalytics(this StorageAccount storageAccount)
        {
            var storageCreds = new StorageCredentials(storageAccount.StorageAccountName, storageAccount.StorageAccountKey);
            var cloudStorageAccount = new CloudStorageAccount(storageCreds, useHttps: true);
            return Task.WhenAll(
                EnableQueueStorageAnalytics(cloudStorageAccount),
                EnableBlobStorageAnalytics(cloudStorageAccount),
                EnableTableStorageAnalytics(cloudStorageAccount),
                EnableFileStorageAnalytics(cloudStorageAccount)
                );
        }

        public static async Task<bool> IsStorageAnalyticsEnabled(this StorageAccount storageAccount)
        {
            try
            {
                var storageCreds = new StorageCredentials(storageAccount.StorageAccountName, storageAccount.StorageAccountKey);
                var cloudStorageAccount = new CloudStorageAccount(storageCreds, useHttps: true);
                var result = await Task.WhenAll(
                    IsQueueStorageAnalyticsEnabled(cloudStorageAccount),
                    IsEnableBlobStorageAnalyticsEnabled(cloudStorageAccount),
                    IsEnableTableStorageAnalyticsEnabled(cloudStorageAccount),
                    IsEnableFileStorageAnalyticsEnabled(cloudStorageAccount)
                    );
                return result.All(i => i);
            }
            catch (Exception e)
            {
                // TODO: log it
                return false;
            }
        }

        

        private static async Task EnableFileStorageAnalytics(CloudStorageAccount storageAccount)
        {
            var fileClient = storageAccount.CreateCloudFileClient();
            var serviceProperties = await fileClient.GetServicePropertiesAsync();

            serviceProperties.MinuteMetrics.MetricsLevel = DefaultMetricsLevel;
            serviceProperties.MinuteMetrics.RetentionDays = DefaultRetentionDays;

            await fileClient.SetServicePropertiesAsync(serviceProperties);
        }

        private static Task EnableTableStorageAnalytics(CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            return setServiceProperties(tableClient.GetServicePropertiesAsync, tableClient.SetServicePropertiesAsync);
        }

        private static Task EnableBlobStorageAnalytics(CloudStorageAccount storageAccount)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            return setServiceProperties(blobClient.GetServicePropertiesAsync, blobClient.SetServicePropertiesAsync);
        }

        private static Task EnableQueueStorageAnalytics(CloudStorageAccount storageAccount)
        {
            var queueClient = storageAccount.CreateCloudQueueClient();
            return setServiceProperties(queueClient.GetServicePropertiesAsync, queueClient.SetServicePropertiesAsync);
        }

        private static Task<bool> IsQueueStorageAnalyticsEnabled(CloudStorageAccount storageAccount)
        {
            var queueClient = storageAccount.CreateCloudQueueClient();
            return IsAnalyticsEnabled(queueClient.GetServicePropertiesAsync, queueClient.SetServicePropertiesAsync);
        }

        private static Task<bool> IsEnableBlobStorageAnalyticsEnabled(CloudStorageAccount storageAccount)
        {
            var blobClient = storageAccount.CreateCloudBlobClient();
            return IsAnalyticsEnabled(blobClient.GetServicePropertiesAsync, blobClient.SetServicePropertiesAsync);
        }

        private static Task<bool> IsEnableTableStorageAnalyticsEnabled(CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            return IsAnalyticsEnabled(tableClient.GetServicePropertiesAsync, tableClient.SetServicePropertiesAsync);
        }

        private static async Task<bool> IsEnableFileStorageAnalyticsEnabled(CloudStorageAccount storageAccount)
        {
            var fileClient = storageAccount.CreateCloudFileClient();
            var serviceProperties = await fileClient.GetServicePropertiesAsync();

            return serviceProperties.MinuteMetrics.MetricsLevel == DefaultMetricsLevel &&
            serviceProperties.MinuteMetrics.RetentionDays == DefaultRetentionDays;
        }

        private static async Task setServiceProperties(Func<Task<ServiceProperties>> getServiceProperties, Func<ServiceProperties, Task> setServiceProperties)
        {
            var serviceProperties = await getServiceProperties();
            serviceProperties.MinuteMetrics.MetricsLevel = DefaultMetricsLevel;
            serviceProperties.MinuteMetrics.RetentionDays = DefaultRetentionDays;
            await setServiceProperties(serviceProperties);
        }

        private static async Task<bool> IsAnalyticsEnabled(Func<Task<ServiceProperties>> getServiceProperties, Func<ServiceProperties, Task> setServiceProperties)
        {
            var serviceProperties = await getServiceProperties();
            return serviceProperties.MinuteMetrics.MetricsLevel == DefaultMetricsLevel &&
                serviceProperties.MinuteMetrics.RetentionDays == DefaultRetentionDays;
        }
    }
}