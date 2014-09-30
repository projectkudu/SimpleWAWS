using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Management.WebSites.Models;

namespace SimpleWAWS.Code
{
    class Util
    {
        public static WebSiteUpdateConfigurationParameters CreateWebSiteUpdateConfigurationParameters()
        {
            // It's important to null out the collections to avoid deleting things we don't want to set
            return new WebSiteUpdateConfigurationParameters
            {
                AppSettings = null,
                ConnectionStrings = null,
                DefaultDocuments = null,
                HandlerMappings = null,
                Metadata = null
            };
        }

        public static async Task SafeGuard(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

    }
}
