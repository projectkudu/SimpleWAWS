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

    }
}
