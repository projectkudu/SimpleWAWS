using System.Web.Http.ExceptionHandling;

namespace SimpleWAWS
{
    public class TelemetryExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            AppInsights.TelemetryClient.TrackException(context.Exception);
        }
    }
}