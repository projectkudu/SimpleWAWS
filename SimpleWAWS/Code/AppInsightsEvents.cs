using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Results;

namespace SimpleWAWS.Code
{
    public static class AppInsightsEvents
    {
        public static class UserActions
        {
            public const string DownloadPublishingProfile = "UserActions/DownloadPublishingProfile";
            public const string CreateWebsite = "UserActions/CreateWebsite";
            public const string DeleteWebsite = "UserActions/DeleteWebsite";
        }

        public static class UserErrors
        {
            public const string MoreThanOneWebsite = "UserErrors/MoreThanOneWebsite";
        }

        public static class ServerErrors
        {
            public const string GeneralException = "ServerErrors/GeneralException";
            public const string NoFreeSites = "ServerErrors/NoFreeSites";
        }

        public static class ServerStatistics
        {
            public const string NumberOfFreeSites = "ServerStatistics/NumberOfFreeSites";
            public const string NumberOfUsedSites = "ServerStatistics/NumberOfUsedSites";
        }
    }
}