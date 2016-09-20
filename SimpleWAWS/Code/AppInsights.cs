using Microsoft.ApplicationInsights;

namespace SimpleWawsService
{
    public class AppInsights{

        private static TelemetryClient _instance;
        public static TelemetryClient TelemetryClient => _instance ?? (_instance = new TelemetryClient());
    }
}