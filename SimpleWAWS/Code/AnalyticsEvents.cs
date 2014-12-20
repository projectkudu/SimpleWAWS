
namespace SimpleWAWS.Code
{
    public static class AnalyticsEvents
    {
        // Event format "###### User {userName} logged in, session created"
        public const string OldUserLoggedIn = "######";

        // Event format "USER_LOGGED_IN; {userName}"
        public const string UserLoggedIn = "USER_LOGGED_IN";

        // Event format "### User {userName} got error {error}"
        public const string OldUserGotError = "###";

        public const string UserGotError = "USER_GOT_ERROR";

        // Event format "##### User {userName}, created site language {language}, template {template}"
        public const string UserCreatedSiteWithLanguageAndTemplateName = "#####";

        public const string UserPuidValue = "USER_PUID_VALUE";
        public const string ApplicationStarted = ">>>>>>>";
    }
}