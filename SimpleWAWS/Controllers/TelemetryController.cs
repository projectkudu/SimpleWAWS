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
        public HttpResponseMessage LogEvent(TelemetryEvent telemetryEvent)
        {
            var userName = User != null && User.Identity != null && !string.IsNullOrEmpty(User.Identity.Name)
                ? User.Identity.Name
                : "-";
            SimpleTrace.TraceInformation("{0}; {1}", AnalyticsEvents.TelemetryEventsMap[telemetryEvent], userName);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}