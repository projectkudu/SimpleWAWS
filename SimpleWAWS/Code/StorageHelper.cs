using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using SimpleWAWS.Code;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System;

namespace SimpleWAWS.Models
{
    public static class StorageHelper
    {
        private static readonly CloudStorageAccount StorageAccount;
//        private static readonly CloudBlobClient BlobClient;
        private static readonly CloudTableClient TableClient;
        private static readonly CloudQueueClient QueueClient;
        static StorageHelper()
        {
            if (!string.IsNullOrEmpty(SimpleSettings.StorageConnectionString))
            {
                StorageAccount = CloudStorageAccount.Parse(SimpleSettings.StorageConnectionString);
                TableClient = StorageAccount.CreateCloudTableClient();
                QueueClient = StorageAccount.CreateCloudQueueClient();
            }
        }

        //public static async Task UploadBlob(string containerName, string blobName, Stream contentStream)
        //{
        //    if (!string.IsNullOrEmpty(containerName))
        //    {
        //        var container = BlobClient.GetContainerReference(containerName);
        //        await container.CreateIfNotExistsAsync();
        //        var blob = container.GetBlockBlobReference(blobName);
        //        await blob.UploadFromStreamAsync(contentStream);
        //    }
        //}
        public static async Task AddQueueMessageIfNotPresent(string queueName, object message)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                queue.CreateIfNotExists();
                await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(message)), TimeSpan.FromSeconds(-1), null, null, null);
            }
        }
        public static async Task AddQueueMessage(string queueName, ResourceGroup message)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                queue.CreateIfNotExists();
                if (!(await PeekQueueMessages(queueName)).Any(c => c.CsmId.Equals(message.CsmId, StringComparison.OrdinalIgnoreCase))
                    && await GetQueueCount(queueName) <= SimpleSettings.MaxFreeSitesQueueLength)
                {
                    await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(message)), TimeSpan.FromSeconds(-1), null, null, null);
                }
            }
        }
        public static async Task<ResourceGroup> GetQueueMessage(string queueName)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync();
                var message = await queue.GetMessageAsync();
                if (message != null && message.PopReceipt != null)
                {   ///TODO: move this to after the message has been consumed
                    await DeleteQueueMessage(queue, message.Id, message.PopReceipt);
                    return JsonConvert.DeserializeObject<ResourceGroup>(message.AsString);
                }
                return null;
            }
            return null;
        }
        public static async Task DeleteQueueMessage(CloudQueue queue, string messageId, string popReceipt)
        {
                await queue.DeleteMessageAsync(messageId, popReceipt);
        }
        public static async Task<List<ResourceGroup>> PeekQueueMessages(string queueName)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync();
                var messages = (await queue.PeekMessagesAsync(SimpleSettings.MaxFreeSitesQueueLength + 1)).ToList();
                return messages.Select(a => JsonConvert.DeserializeObject<ResourceGroup>(a.AsString)).ToList();
            }
            return null;
        }
        public static async Task<List<ResourceGroup>> GetAllFreeResources()
        {
            var queueMessages = new List<ResourceGroup>();
            List<Task<IEnumerable<CloudQueueMessage>>> messageCollector = new List<Task<IEnumerable<CloudQueueMessage>>>();
            foreach (var queue in await ListFreeQueues())
            {
                messageCollector.Add(queue.PeekMessagesAsync(SimpleSettings.MaxFreeSitesQueueLength + 1));
            }
            var result = await messageCollector.WhenAll();
            //List<Task<IEnumerable<ResourceGroup>>> rgConverter = new List<Task<IEnumerable<ResourceGroup>>>();
            foreach (var set in result)
            {
                foreach (var rg in set)
                {
                    queueMessages.Add(JsonConvert.DeserializeObject<ResourceGroup>(rg.AsString));
                }
            }
            return queueMessages;
        }
        public static async Task<List<CloudQueue>> ListFreeQueues()
        {
            // list the queues in the account
            var queues = new List<CloudQueue>();
            QueueContinuationToken continuationToken = null;
            do
            {
                var segment = await QueueClient.ListQueuesSegmentedAsync("free-", continuationToken);
                queues.AddRange(segment.Results);
                continuationToken = segment.ContinuationToken;
            } while (continuationToken != null);

            return queues;
        }
        public static async Task<int?> GetQueueCount(string queueName)
        {
            if (!string.IsNullOrEmpty(queueName))
            {
                var queue = QueueClient.GetQueueReference(queueName);
                await queue.CreateIfNotExistsAsync();
                await queue.FetchAttributesAsync();
                return queue.ApproximateMessageCount;
            }
            return null;
        }

        public static async Task<ConcurrentDictionary<string, ResourceGroup>> GetInUseResourceGroups()
        {
            var table = TableClient.GetTableReference(SimpleSettings.InUseResourceTableName);
            await table.CreateIfNotExistsAsync();
            TableContinuationToken token = null;
            var entities = new ConcurrentDictionary<string, ResourceGroup>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<InUseResourceEntity> { FilterString = $"PartitionKey eq '{SimpleSettings.PartitionKey}'",  SelectColumns = new List<string> { "RowKey", "ResourceGroup" } }, token);
                foreach (var entry in queryResult.Results)
                {
                    if (entry.ResourceGroup != null)
                    {
                        entities.GetOrAdd(entry.RowKey, JsonConvert.DeserializeObject<ResourceGroup>(entry.ResourceGroup));
                    }
                }
                token = queryResult.ContinuationToken;
            } while (token != null);

            return entities;
        }

        public static async Task<ResourceGroup> GetAssignedResourceGroup(string userName)
        {
                var table = TableClient.GetTableReference(SimpleSettings.InUseResourceTableName);
                TableContinuationToken token = null;
                var entities = new ConcurrentDictionary<string, ResourceGroup>();
                do
                {
                    var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<InUseResourceEntity> { SelectColumns = new List<string> { "ResourceGroup" }, FilterString = $"PartitionKey eq '{SimpleSettings.PartitionKey}' and RowKey eq '{userName}'", TakeCount = 1 }, token);
                    token = queryResult.ContinuationToken;
                    return queryResult.Results.Count > 0 ? JsonConvert.DeserializeObject<ResourceGroup>(queryResult.Results[0].ResourceGroup) : null;
                } while (token != null);
        }

        public static async Task<bool> UnAssignResourceGroup(string userName)
        {
            var table = TableClient.GetTableReference(SimpleSettings.InUseResourceTableName);
            TableContinuationToken token = null;
            var entities = new ConcurrentDictionary<string, ResourceGroup>();
            do
            {
                var queryResult = await table.ExecuteQuerySegmentedAsync(new TableQuery<InUseResourceEntity> { FilterString = $"PartitionKey eq '{SimpleSettings.PartitionKey}' and RowKey eq '{userName}'", TakeCount = 1 }, token);
                token = queryResult.ContinuationToken;
                foreach (var item in queryResult)
                {
                    var oper = TableOperation.Delete(item);
                    table.Execute(oper);
                    return true;
                }
            } while (token != null);
            return false;
        }
        public static async Task<bool> AssignResourceGroup(string userName, ResourceGroup rg)
        {
            try
            {
                var table = TableClient.GetTableReference(SimpleSettings.InUseResourceTableName);

                var oper = TableOperation.InsertOrReplace(
                    new InUseResourceEntity { PartitionKey = SimpleSettings.PartitionKey, RowKey = userName, ResourceGroup =JsonConvert.SerializeObject(rg) });
                var x = await table.ExecuteAsync(oper);
                if (x.Result != null)
                    return true;
                else
                    return false;
            } catch (Exception ex)
            {
                return false;
            }
        }
    }
}