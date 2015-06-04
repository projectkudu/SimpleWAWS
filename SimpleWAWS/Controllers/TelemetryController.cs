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

                SimpleTrace.Analytics.Information(AnalyticsEvents.UiEvent, telemetryEvent, properties);

                var dic = properties != null
                    ? properties.ToObject<Dictionary<string, string>>().Select(e => e.Value).Aggregate((a, b) => string.Join(" ", a, b))
                    : string.Empty;
                SimpleTrace.TraceInformation("{0}; {1}; {2}; {3}", AnalyticsEvents.OldUiEvent, telemetryEvent, userName, dic);
            }
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}