using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TryAppService.Data.DataAccess;
using TryAppService.Data.Models;

namespace TryAppService.WebJob.Aggregation
{
    public class ApplicationLogAnalyzer : IDisposable
    {
        //TODO: reduce code duplication
        private readonly TryItNowAnalyticsContext _tryItNowAnalyticsContext;
        private readonly StorageHelper _storageHelper;

        private const string OldUserCreatesSitePattern = ",\"##### User ";
        private const string UserCreatesSitePattern = "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE";
        private const string UserPuidPattern = "USER_PUID_VALUE";
        private const string UserClickedFreeTrial = "FREE_TRIAL_CLICK";
        private const string UiEvent = "UI_EVENT";
        private const string UserGotError = "USER_GOT_ERROR";
        private const string AnonymousUserCreated = "ANONYMOUS_USER_CREATED";
        private const string AnonymousUserLoggedIn = "ANONYMOUS_USER_LOGGEDIN";
        private const string AnonymousUserInit = "ANONYMOUS_USER_INIT";
        private const string UserFeedbackPattern = "FEEDBACK_COMMENT";

        public ApplicationLogAnalyzer()
        {
            this._tryItNowAnalyticsContext = new TryItNowAnalyticsContext();
            this._storageHelper = new StorageHelper("wawsapplogblobtrywebsites");
        }

        private Tuple<string, string, string> ParseExperimentAndSvAndCulture(string str)
        {
            if (str.IndexOf("#") == -1) return Tuple.Create("-", "-", "-");
            var value = str.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries).Last();

            if (str.IndexOf("$") == -1) return Tuple.Create(value, "-", "-");
            var splits = value.Split('$');

            if (splits[1].IndexOf("%") == -1) return Tuple.Create(splits[0], splits[1], "-");
            var splits2 = splits[1].Split('%');
            return Tuple.Create(splits[0], splits2[0], splits2[1]);
        }

        public void Analyze()
        {
            DateTime startHour;
            var endHour = GetCurrentHour();
            if (TryGetStartHourForAnalysis(endHour, out startHour))
            {
                for (var currentHour = startHour; currentHour < endHour; currentHour = currentHour.AddHours(1))
                {
                    Console.WriteLine(currentHour);
                    foreach (var stream in this._storageHelper.GetLogFilesForHour("trywebsites", currentHour))
                    {
                        var userActivity = new List<UserActivity>();
                        var userPuids = new HashSet<UserPuid>();
                        var uiEvents = new List<UIEvent>();
                        var userAssignedExperiment = new List<UserAssignedExperiment>();
                        var userLoggedIn = new List<UserLoggedIn>();
                        var userFeedback = new List<UserFeedback>();
                        try
                        {
                            using (var reader = new StreamReader(stream))
                            {

                                while (!reader.EndOfStream)
                                {
                                    var line = reader.ReadLine();
                                    if (line == null)
                                        continue;
                                    try
                                    {
                                        var splitedLine = line.Split(';').Select(s => s.Trim(',', ' ', '"')).ToArray();
                                        var experimentsAndSvAndCulture = ParseExperimentAndSvAndCulture(splitedLine[0]);
                                        if (line.IndexOf(OldUserCreatesSitePattern) != -1)
                                        {
                                            splitedLine = line.Split(',').Select(s => s.Trim(',', ' ', '"')).ToArray();
                                            userActivity.Add(new UserActivity
                                            {
                                                DateTime = currentHour,
                                                TemplateLanguage = splitedLine[9].Split(' ').LastOrDefault(),
                                                TemplateName = splitedLine[10].Replace("template", ""),
                                                UserName = splitedLine[8].Replace(OldUserCreatesSitePattern.Substring(1), "")
                                            });
                                        }
                                        else if (line.IndexOf(UserPuidPattern) != -1)
                                        {
                                            if (userPuids.FirstOrDefault(p => p.Puid == splitedLine[2]) == null)
                                            {
                                                userPuids.Add(new UserPuid
                                                {
                                                    UserName = splitedLine[1],
                                                    Puid = splitedLine[2]
                                                });
                                            }
                                        }
                                        else if (line.IndexOf(UiEvent) != -1)
                                        {
                                            uiEvents.Add(new UIEvent
                                            {
                                                DateTime = currentHour,
                                                Experiment = experimentsAndSvAndCulture.Item1,
                                                SourceVariation = experimentsAndSvAndCulture.Item2,
                                                UserCulture = experimentsAndSvAndCulture.Item3,
                                                EventName = splitedLine[1],
                                                UserName = splitedLine[2],
                                                Properties = splitedLine.Length > 3 ? splitedLine[3] : null,
                                                AnonymousUserName = splitedLine.Length > 4 ? splitedLine[4] : null
                                            });
                                        }
                                        else if (line.IndexOf(UserClickedFreeTrial) != -1)
                                        {
                                            uiEvents.Add(new UIEvent
                                            {
                                                DateTime = currentHour,
                                                Experiment = experimentsAndSvAndCulture.Item1,
                                                SourceVariation = experimentsAndSvAndCulture.Item2,
                                                UserCulture = experimentsAndSvAndCulture.Item3,
                                                UserName = splitedLine[1],
                                                EventName = UserClickedFreeTrial,
                                                Properties = splitedLine.Length > 2 ? splitedLine[2] : null
                                            });
                                        }
                                        else if (line.IndexOf(UserCreatesSitePattern) != -1)
                                        {
                                            userActivity.Add(new UserActivity
                                            {
                                                DateTime = currentHour,
                                                Experiment = experimentsAndSvAndCulture.Item1,
                                                SourceVariation = experimentsAndSvAndCulture.Item2,
                                                UserCulture = experimentsAndSvAndCulture.Item3,
                                                UserName = splitedLine[1],
                                                TemplateLanguage = splitedLine[2],
                                                TemplateName = splitedLine[3],
                                                UniqueId = splitedLine[4],
                                                AppService = splitedLine.Length > 5 ? splitedLine[5] : splitedLine[2] == "Mobile" || string.IsNullOrWhiteSpace(splitedLine[2]) ? "Mobile" : "Web",
                                                AnonymousUserName = splitedLine.Length > 6 ? splitedLine[6] : null
                                            });
                                        }
                                        else if (line.IndexOf(AnonymousUserLoggedIn) != -1)
                                        {
                                            userLoggedIn.Add(new UserLoggedIn
                                            {
                                                DateTime = currentHour,
                                                AnonymousUserName = splitedLine[1].StartsWith("Anonymous#") ? splitedLine[1] : "Anonymous#" + splitedLine[1],
                                                LoggedInUserName = splitedLine[2]
                                            });
                                        }
                                        else if (line.IndexOf(AnonymousUserCreated) != -1 ||
                                                 line.IndexOf(AnonymousUserInit) != -1)
                                        {
                                            userAssignedExperiment.Add(new UserAssignedExperiment
                                            {
                                                DateTime = currentHour,
                                                UserName = splitedLine[1].StartsWith("Anonymous#") ? splitedLine[1] : "Anonymous#" + splitedLine[1],
                                                Referer = splitedLine.Length > 3 ? splitedLine[3] : "-",
                                                CampaignId = splitedLine.Length > 4 ? splitedLine[4] : "-",
                                                Experiment = experimentsAndSvAndCulture.Item1,
                                                SourceVariation = experimentsAndSvAndCulture.Item2,
                                                UserCulture = experimentsAndSvAndCulture.Item3
                                            });
                                        }
                                        else if (line.IndexOf(UserFeedbackPattern) != -1)
                                        {
                                            bool contactMe;
                                            if (!bool.TryParse(splitedLine[4], out contactMe))
                                                contactMe = false;
                                            userFeedback.Add(new UserFeedback
                                            {
                                                DateTime = currentHour,
                                                Experiment = experimentsAndSvAndCulture.Item1,
                                                SourceVariation = experimentsAndSvAndCulture.Item2,
                                                UserCulture = experimentsAndSvAndCulture.Item3,
                                                UserName = splitedLine[1],
                                                AnonymousUserName = splitedLine[2],
                                                Comment = splitedLine[3],
                                                ContactMe = contactMe
                                            });
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Trace.TraceError(e.ToString());
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
                            SaveHourAggregateInDataBase(userActivity, userPuids, uiEvents, userLoggedIn, userAssignedExperiment, userFeedback);
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

        private void SaveHourAggregateInDataBase(IEnumerable<UserActivity> userActivity, IEnumerable<UserPuid> userPuids, IEnumerable<UIEvent> uiEvents, IEnumerable<UserLoggedIn> userLoggedIn, IEnumerable<UserAssignedExperiment> userAssignedExperiments, IEnumerable<UserFeedback> userFeedback)
        {
            this._tryItNowAnalyticsContext.UserActivities.AddRange(userActivity);
            this._tryItNowAnalyticsContext.UserPuids.AddOrUpdate(p => p.Puid, userPuids.ToArray());
            this._tryItNowAnalyticsContext.UIEvents.AddRange(uiEvents);
            this._tryItNowAnalyticsContext.UserLoggedIns.AddRange(userLoggedIn);
            this._tryItNowAnalyticsContext.UserAssignedExperiments.AddRange(userAssignedExperiments);
            this._tryItNowAnalyticsContext.UserFeedback.AddRange(userFeedback);

            this._tryItNowAnalyticsContext.SaveChanges();
        }

        private bool TryGetStartHourForAnalysis(DateTime currentHour, out DateTime startHour)
        {
            //TODO: any added tables will have to be filled back manually. I can't think of a better way for now
            var userActivity = this._tryItNowAnalyticsContext.UserActivities.OrderByDescending(ua => ua.DateTime).FirstOrDefault();
            if (userActivity == null)
            {
                //we have an empty database, fill everything since we went public 7/31/2014 16:00:00 UTC
                startHour = Constants.CreationMoment;
            }
            else
            {
                if ((currentHour - userActivity.DateTime).TotalHours > 0)
                {
                    startHour = userActivity.DateTime.Add(TimeSpan.FromHours(1));
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
