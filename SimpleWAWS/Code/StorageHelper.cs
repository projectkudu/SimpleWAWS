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

namespace SimpleWAWS.Models
{
    public static class StorageHelper
    {
        private static readonly CloudStorageAccount StorageAccount;
        private static readonly CloudBlobClient BlobClient;
        private static readonly CloudQueueClient QueueClient;
        static StorageHelper()
        {
            StorageAccount = CloudStorageAccount.Parse(SimpleSettings.StorageConnectionString);
            BlobClient = StorageAccount.CreateCloudBlobClient();
            QueueClient = StorageAccount.CreateCloudQueueClient();
        }

        public static async Task UploadBlob(string blobName, Stream contentStream)
        {
            var container = BlobClient.GetContainerReference(SimpleSettings.FreeSitesIISLogsBlob);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(blobName);
            await blob.UploadFromStreamAsync(contentStream);
        }

        public static async Task AddQueueMessage(object message)
        {
            var queue = QueueClient.GetQueueReference(SimpleSettings.FreeSitesIISLogsQueue);
            await queue.CreateIfNotExistsAsync();
            await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(message)));
        }
    }
}