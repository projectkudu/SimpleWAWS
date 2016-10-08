using Serilog;
using SimpleWAWS.Code;
using System.Globalization;
using System.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Authentication;

namespace SimpleWAWS.Trace
{
    public static class SimpleTrace
    {
        public static ILogger Analytics;
        public static ILogger Diagnostics;

        static SimpleTrace()
        {
        }

        public static void TraceInformation(string message)
        {
            System.Diagnostics.Trace.TraceInformation(message);
        }

        public static void TraceInformation(string format, params string[] args)
        {
            if (args.Length > 0)
            {
                args[0] = string.Concat(args[0], "#", ExperimentManager.GetCurrentExperiment(), "$", ExperimentManager.GetCurrentSourceVariation(), "%", CultureInfo.CurrentCulture.EnglishName);
            }
            args = args.Select(e => e?.Replace(";", "&semi")).ToArray();

            System.Diagnostics.Trace.TraceInformation(format, args);
        }

        public static void TraceError(string message)
        {
            System.Diagnostics.Trace.TraceError(message);
        }

        public static void TraceError(string format, params string[] args)
        {
            if (args.Length > 0)
            {
                args[0] = string.Concat(args[0], "#", ExperimentManager.GetCurrentExperiment(), "$", ExperimentManager.GetCurrentSourceVariation(), "%", CultureInfo.CurrentCulture.EnglishName);
            }

            System.Diagnostics.Trace.TraceError(format, args);
        }

        public static void TraceWarning(string message)
        {
            System.Diagnostics.Trace.TraceWarning(message);
        }

        public static void TraceWarning(string format, params string[] args)
        {
            if (args.Length > 0)
            {
                args[0] = string.Concat(args[0], "#", ExperimentManager.GetCurrentExperiment(), "$", ExperimentManager.GetCurrentSourceVariation(), "%", CultureInfo.CurrentCulture.EnglishName);
            }

            System.Diagnostics.Trace.TraceWarning(format, args);
        }

        public static void InitializeAnonymousUser(string userName, string experiment, string referer, string campaignId, string sourceVariation)
        {
            // Create a new Anonymous user object.
        }

        public static void UserCreatedApp(TryWebsitesIdentity userIdentity, BaseTemplate template, ResourceGroup resourceGroup, AppService logic)
        {
            // Get the current journey for the user and add an app and update count.
        }

        public static void AnonymousUserLoggedIn(TryWebsitesIdentity anonymousIdentity, TryWebsitesIdentity identity)
        {
            // Get the anonymous journey for the user and create a logged in user journey for it.
        }

        internal static void ExtendResourceGroup(ResourceGroup resourceGroup)
        {
            // Get journey and update current resource with extension
        }
    }
}
