using Microsoft.Azure.WebJobs;
using System;
using System.Data.Entity.Migrations;
using System.IO;
using System.IO.Compression;
using TryAppService.Data.DataAccess;
using TryAppService.Data.Models;

namespace TryAppService.WebJob.ProcessQueue
{
    public class Functions
    {
        public static void ProcessQueue([QueueTrigger("freesitesiislogsqueue")] BlobInformation blobInfo,
                                        [Blob("freesitesiislogs/{BlobName}", FileAccess.Read)] Stream input)
        {
            try
            {
                var siteUsageTime = GetSiteUsageTime(blobInfo.BlobName, input);
                using (var context = new TryItNowAnalyticsContext())
                {
                    context.SiteUsageTimes.AddOrUpdate(e => e.UniqueId, siteUsageTime);
                    context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static SiteUsageTime GetSiteUsageTime(string siteUniqueId, Stream zipFileStream)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), siteUniqueId);
            var tempLogFolder = Path.Combine(tempFolder, "LogFiles");
            Directory.CreateDirectory(tempLogFolder);
            try
            {
                var tempFile = Path.Combine(tempFolder, siteUniqueId);
                using (var localZip = File.OpenWrite(tempFile))
                {
                    zipFileStream.CopyTo(localZip);
                }
                ZipFile.ExtractToDirectory(tempFile, tempLogFolder);
                var time = ParseLogs(tempLogFolder);
                return new SiteUsageTime
                {
                    UsageTime = time,
                    UniqueId = Guid.Parse(siteUniqueId)
                };
            }
            finally
            {
                try
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static TimeSpan ParseLogs(string tempLogFolder)
        {
            var startTime = DateTime.MaxValue;
            var endTime = DateTime.MinValue;
            foreach (var file in Directory.GetFiles(tempLogFolder))
            {
                using (var reader = new StreamReader(file))
                {
                    var dateIndex = 1;
                    var timeIndex = 2;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                            continue;

                        var lineSplited = line.Split(' ');
                        if (line.StartsWith("#Fields:"))
                        {
                            for (var i = 0; i < lineSplited.Length; i++)
                            {
                                switch (lineSplited[i])
                                {
                                    case "date":
                                        dateIndex = i - 1;
                                        break;
                                    case "time":
                                        timeIndex = i - 1;
                                        break;
                                }
                            }
                        }

                        if (line.StartsWith("#"))
                            continue;

                        DateTime lineDateTime;
                        if (DateTime.TryParse(string.Format("{0} {1}", lineSplited[dateIndex], lineSplited[timeIndex]), out lineDateTime))
                        {
                            if (lineDateTime < startTime) startTime = lineDateTime;
                            if (lineDateTime > endTime) endTime = lineDateTime;
                        }
                    }
                }
            }

            if (startTime != DateTime.MaxValue && endTime != DateTime.MinValue)
            {
                return endTime - startTime;
            }
            else
            {
                throw new Exception(string.Format("either startTime or endTime wasn't updated. startTime: {0}, endTime: {1}.", startTime, endTime));
            }
        }

    }

    public class BlobInformation
    {
        public string BlobName { get; set; }
    }

}
