using Newtonsoft.Json.Linq;
using SimpleWAWS.Authentication;
using SimpleWAWS.Code;
using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace SimpleWAWS.Controllers
{
    public class TelemetryController : ApiController
    {
        public HttpResponseMessage LogEvent(string telemetryEvent, JObject properties)
        {
            var context = new HttpContextWrapper(HttpContext.Current);
            if (context.IsBrowserRequest())
            {
                var userName = User != null && User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name)
                    ? User.Identity.Name
                    : "-";
                var anonymousUserName = SecurityManager.GetAnonymousUserName(context);

                if (telemetryEvent.Equals("INIT_USER", StringComparison.OrdinalIgnoreCase))
                {
                    var dic = properties != null
                        ? properties.ToObject<Dictionary<string, string>>()
                        : new Dictionary<string, string>();

                    Func<string, string> cleanUp = (s) => string.IsNullOrEmpty(s) ? "-" : s;
                    var referer = cleanUp(dic.Where(p => p.Key == "origin").Select(p => p.Value).FirstOrDefault());
                    var cid = cleanUp(dic.Where(p => p.Key == "cid").Select(p => p.Value).FirstOrDefault());
                    var sv = cleanUp(dic.Where(p => p.Key == "sv").Select(p => p.Value).FirstOrDefault());

                    SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.AnonymousUserInit,
                            "",
                            referer,
                            cid,
                            sv
                        );
                    SimpleTrace.InitializeAnonymousUser(userName, "", referer, cid, sv);
                }
                else
                {
                    SimpleTrace.Analytics.Information(AnalyticsEvents.UiEvent, telemetryEvent, properties);

                    var eventProperties = properties != null
                        ? properties.ToObject<Dictionary<string, string>>().Select(e => e.Value).Aggregate((a, b) => string.Join(",", a, b))
                        : string.Empty;
                    SimpleTrace.TraceInformation("{0}; {1}; {2};", AnalyticsEvents.OldUiEvent, telemetryEvent, eventProperties);
                }
            }
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }

        public HttpResponseMessage LogFeedback(Feedback feedback)
        {
            if (feedback != null && !string.IsNullOrWhiteSpace(feedback.Comment))
            {
                var context = new HttpContextWrapper(HttpContext.Current);
                var userName = User != null && User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name)
                    ? User.Identity.Name
                    : "-";
                var anonymousUserName = SecurityManager.GetAnonymousUserName(context);
                SimpleTrace.TraceInformation("{0}; {1}; {2};",
                    AnalyticsEvents.FeedbackComment,
                    feedback.Comment.Replace(';', '_'),
                    feedback.ContactMe.ToString());
            }
            return Request.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}