using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TryAppService.Data.DataAccess;
using TryAppService.Data.Models;

namespace TryAppService.WebJob.Aggregation
{
    public class IISLogAnalyzer : IDisposable
    {
        private readonly TryItNowAnalyticsContext _tryItNowAnalyticsContext;
        private readonly StorageHelper _storageHelper;
        public IISLogAnalyzer()
        {
            this._tryItNowAnalyticsContext = new TryItNowAnalyticsContext();
            this._storageHelper = new StorageHelper("wawssitelogblobtrywebsites");
        }
        public void Analyze()
        {
            DateTime startHour;
            var endHour = GetCurrentHour();
            if (TryGetStartHourForAnalysis(endHour, out startHour))
            {
                for (var currentHour = startHour; currentHour < endHour; currentHour = currentHour.AddHours(1))
                {
                    foreach (var stream in this._storageHelper.GetLogFilesForHour("TRYWEBSITES", currentHour))
                    {
                        var requestAggregate = new Dictionary<string, int>();
                        var usersAggregate = new Dictionary<string, int>();
                        var httpStatusAggregate = new Dictionary<int, int>();
                        var refererAggregate = new Dictionary<string, int>();
                        try
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                var siteNameIndex = 2;
                                var pathIndex = 4;
                                var userIndex = 7;
                                var httpStatusIndex = 13;
                                var refererIndex = 11;
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
                                                case "s-sitename":
                                                    siteNameIndex = i - 1;
                                                    break;
                                                case "cs-uri-stem":
                                                    pathIndex = i - 1;
                                                    break;
                                                case "cs-username":
                                                    userIndex = i - 1;
                                                    break;
                                                case "sc-status":
                                                    httpStatusIndex = i - 1;
                                                    break;
                                                case "cs(Referer)":
                                                    refererIndex = i - 1;
                                                    break;
                                            }
                                        }
                                    }

                                    if (line.StartsWith("#") || lineSplited[siteNameIndex].StartsWith("~1"))
                                        continue;

                                    //Request Count
                                    requestAggregate.IncrementOrCreate(lineSplited[pathIndex]);

                                    //User in hour
                                    usersAggregate.IncrementOrCreate(lineSplited[userIndex]);

                                    //http status
                                    int status;
                                    if (int.TryParse(lineSplited[httpStatusIndex], out status))
                                    {
                                        httpStatusAggregate.IncrementOrCreate(status);
                                    }

                                    var ignoreList = new[] 
                                { "://www.facebook.com/dialog/oauth",
                                  "://www.facebook.com/v2.2/dialog/oauth",
                                  "://m.facebook.com/v2.2/dialog/oauth",
                                  "://www.facebook.com/login.php",
                                  "://trywebsites.azurewebsites.net",
                                  "://tryappservice.azure.com",
                                  "://login.microsoftonline.com",
                                  "://login.live.com",
                                  "://accounts.google.com/o/oauth2/auth",
                                  "://msft.sts.microsoft.com/adfs/ls/"
                                };

                                    //if any of the items in the list matches the current entry, return true
                                    if (!ignoreList.Any(l => lineSplited[refererIndex].IndexOf(l, StringComparison.OrdinalIgnoreCase) != -1) &&
                                        lineSplited[refererIndex] != "-")
                                    {
                                        refererAggregate.IncrementOrCreate(lineSplited[refererIndex]);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError(e.ToString());
                        }
                        finally
                        {
                            SaveHourAggregateInDataBase(requestAggregate, usersAggregate, refererAggregate, httpStatusAggregate, currentHour);
                            try
                            {
                                stream.Dispose();
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
        }

        private void SaveHourAggregateInDataBase(Dictionary<string, int> requestAggregate,
            Dictionary<string, int> usersAggregate, Dictionary<string, int> refererAggregate,
            Dictionary<int, int> httpStatusAggregate, DateTime hour)
        {
            this._tryItNowAnalyticsContext.RequestsAggregates.AddRange(
                requestAggregate.Select(ra => new RequestsAggregate() { Path = ra.Key, Hits = ra.Value, Hour = hour }));
            this._tryItNowAnalyticsContext.UserHourAggregates.AddRange(
                usersAggregate.Select(
                    ua => new UserHourAggregate() { UserName = ua.Key, RequestsCount = ua.Value, Hour = hour }));
            this._tryItNowAnalyticsContext.HttpStatusAggregates.AddRange(
                httpStatusAggregate.Select(
                    ha => new HttpStatusHourlyAggregate() { StatusCode = ha.Key, Count = ha.Value, Hour = hour }));
            this._tryItNowAnalyticsContext.RefererAggregates.AddRange(
                refererAggregate.Select(ra => new RefererAggregate() { Path = ra.Key, Count = ra.Value, Hour = hour }));
            this._tryItNowAnalyticsContext.SaveChanges();
        }

        private bool TryGetStartHourForAnalysis(DateTime currentHour, out DateTime startHour)
        {
            //TODO: any added tables will have to be filled back manually. I can't think of a better way for now
            var hourlyAggregate = this._tryItNowAnalyticsContext.RequestsAggregates.OrderByDescending(ra => ra.Hour).FirstOrDefault();
            if (hourlyAggregate == null)
            {
                //we have an empty database, fill everything since we went public 7/31/2014 16:00:00 UTC
                startHour = Constants.CreationMoment;
            }
            else
            {
                if ((currentHour - hourlyAggregate.Hour).TotalHours > 0)
                {
                    startHour = hourlyAggregate.Hour.Add(TimeSpan.FromHours(1));
                }
                else
                {
                    startHour = DateTime.MaxValue;
                    return false;
                }
            }
            return true;
        }

        private static DateTime GetCurrentHour()
        {
            return new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0);
        }

        public void Dispose()
        {
            this._tryItNowAnalyticsContext.Dispose();
        }
    }
}
