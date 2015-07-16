using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TryAppService.WebJob.Aggregation
{
    public class StorageHelper
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudBlobClient _blobClient;
        private readonly CloudBlobContainer _blobContainer;
        public StorageHelper(string containerName)
        {
            _storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["storageConnectionString"]);
            _blobClient = _storageAccount.CreateCloudBlobClient();
            _blobContainer = _blobClient.GetContainerReference(containerName);
        }

        public IEnumerable<Stream> GetLogFilesForHour(string blobPrefix, DateTime hour)
        {
            var prefix = string.Format("{0}/{1}/{2:00}/{3:00}/{4:00}/", blobPrefix, hour.Year, hour.Month, hour.Day, hour.Hour);
            foreach (var item in _blobContainer.ListBlobs(prefix, useFlatBlobListing: true))
            {
                if (item is CloudBlockBlob)
                {
                    var memoryStream = new MemoryStream();
                    var blobReference = _blobContainer.GetBlockBlobReference(item.Uri.AbsolutePath.Replace(string.Format("/{0}/", item.Container.Name), ""));
                    blobReference.DownloadToStream(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    yield return memoryStream;
                }
            }
        }
    }
}
