using System.Collections.Generic;
using System.Web.Http.ExceptionHandling;

namespace SimpleWAWS
{
    public class TelemetryExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            var properties = new Dictionary<string, string>
            {{"Uri", context.Request?.RequestUri?.AbsoluteUri},
            { "Method", context.Request?.Method?.ToString()},
            { "Content", context.Request?.Content?.ToString()}
            };
            AppInsights.TelemetryClient.TrackException(context.Exception, properties, null);
        }
    }
}