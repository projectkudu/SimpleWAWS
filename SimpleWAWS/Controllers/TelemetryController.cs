using Newtonsoft.Json.Linq;
using SimpleWAWS.Code;
using SimpleWAWS.Models;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                if (telemetryEvent.Equals("INIT_USER", StringComparison.OrdinalIgnoreCase))
                {
                    var dic = properties != null
                        ? properties.ToObject<Dictionary<string, string>>()
                        : new Dictionary<string, string>();

                    Func<string, string> cleanUp = (s) => string.IsNullOrEmpty(s) ? "-" : s;
                    var referer = cleanUp(dic.Where(p => p.Key == "origin").Select(p => p.Value).FirstOrDefault());
                    var cid =  cleanUp(dic.Where(p => p.Key == "cid").Select(p => p.Value).FirstOrDefault());

                    SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}; {4}",
                            AnalyticsEvents.AnonymousUserInit,
                            userName,
                            ExperimentManager.GetCurrentExperiment(),
                            referer,
                            cid
                        );
                }
                else
                {
                    SimpleTrace.Analytics.Information(AnalyticsEvents.UiEvent, telemetryEvent, properties);

                    var eventProperties = properties != null
                        ? properties.ToObject<Dictionary<string, string>>().Select(e => e.Value).Aggregate((a, b) => string.Join(" ", a, b))
                        : string.Empty;
                    SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}", AnalyticsEvents.OldUiEvent, telemetryEvent, userName, eventProperties);
                }
            }
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}