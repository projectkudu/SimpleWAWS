
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
        public const string OldUserCreatedSiteWithLanguageAndTemplateName = "#####";

        // Event format "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE; {userName}; {language}; {template}; {siteUniqueId}"
        public const string UserCreatedSiteWithLanguageAndTemplateName = "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE";

        public const string UserPuidValue = "USER_PUID_VALUE";
        public const string ApplicationStarted = ">>>>>>>";
        public const string ErrorInRemoveRbacUser = "ERROR_REMOVE_RBAC_USER";
        public const string ErrorInAddRbacUser = "ERROR_ADD_RBAC_USER";
    }
}