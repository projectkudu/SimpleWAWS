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

namespace SimpleWAWS.Code
{
    public static class StorageHelper
    {
        private static readonly CloudStorageAccount StorageAccount;
        private static readonly CloudBlobClient BlobClient;
        private static readonly CloudQueueClient QueueClient;
        static StorageHelper()
        {
            StorageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            BlobClient = StorageAccount.CreateCloudBlobClient();
            QueueClient = StorageAccount.CreateCloudQueueClient();
        }

        public static async Task UploadBlob(string blobName, Stream contentStream)
        {
            var container = BlobClient.GetContainerReference("freesitesiislogs");
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadFromStreamAsync(contentStream);
        }

        public static async Task AddQueueMessage(string message)
        {
            var queue = QueueClient.GetQueueReference("freesitesiislogsqueue");
            await queue.CreateIfNotExistsAsync();
            await queue.AddMessageAsync(new CloudQueueMessage(message));
        }
    }
}