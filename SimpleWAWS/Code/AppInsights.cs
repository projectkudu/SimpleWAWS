using Microsoft.ApplicationInsights;

namespace SimpleWAWS
{
    public class AppInsights{
        public static TelemetryClient TelemetryClient { get; } = new TelemetryClient();
    }
}