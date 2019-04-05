using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
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
            if (!string.IsNullOrEmpty(SimpleSettings.StorageConnectionString))
            {
                StorageAccount = CloudStorageAccount.Parse(SimpleSettings.StorageConnectionString);
                BlobClient = StorageAccount.CreateCloudBlobClient();
                QueueClient = StorageAccount.CreateCloudQueueClient();
            }
        }

        public static async Task UploadBlob(string containerName, string blobName, Stream contentStream)
        {
            if (!string.IsNullOrEmpty(containerName))
            {
                var container = BlobClient.GetContainerReference(containerName);
                await container.CreateIfNotExistsAsync();
                var blob = container.GetBlockBlobReference(blobName);
                await blob.UploadFromStreamAsync(contentStream);
            }
        }

        public static async Task AddQueueMessage(string queueName, object message)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync();
                await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(message)));
            }
        }
        public static async Task<object> GetQueueMessage(string queueName)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync();
                return JsonConvert.DeserializeObject((await queue.GetMessageAsync()).AsString);
            }
            return null;
        }
        public static async Task<int?> GetQueueCount(string queueName)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync();
                return queue.ApproximateMessageCount;
            }
            return null;
        }
    }
}