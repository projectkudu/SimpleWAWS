using System.Linq;
using System.Collections.Generic;
using SimpleWAWS.Code;

namespace SimpleWAWS.Models
{
    public static class AnalyticsEvents
    {
        // Event format "###### User {userName} logged in, session created"
        public const string OldUserLoggedIn = "######";

        // Event format "USER_LOGGED_IN; {userName}"
        public const string UserLoggedIn = "USER_LOGGED_IN; {@user}";

        // Event format "### User {userName} got error {error}"
        public const string OldUserGotError = "###";

        public const string UserGotError = "USER_GOT_ERROR; {user}; {errorMessage}";
        public const string MoreThanOneError = "MORE_THAN_ONE_ERROR; {user}";

        // Event format "##### User {userName}, created site language {language}, template {template}"
        public const string OldUserCreatedSiteWithLanguageAndTemplateName = "#####";

        // Event format "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE; {userName}; {language}; {template}; {siteUniqueId}"
        public const string UserCreatedSiteWithLanguageAndTemplateName = "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE; {@user}; {@template}; {resourceGroupId}";

        public const string UserPuidValue = "USER_PUID_VALUE; {@user}";
        public const string ApplicationStarted = ">>>>>>>";
        public const string ErrorInRemoveRbacUser = "ERROR_REMOVE_RBAC_USER; {resourceGroupId}";
        public const string ErrorInAddRbacUser = "ERROR_ADD_RBAC_USER; {@user}";
        public const string ErrorInCheckRbacUser = "ERROR_CHECK_RBAC_USER; {resourceGroupId}";

        public const string FreeTrialClick = "FREE_TRIAL_CLICK; {user}";
        public const string IbizaClick = "IBIZA_CLICK; {user}";
        public const string MonacoClick = "MONACO_CLICK; {user}";

        public const string RemoveUserFromTenant = "REMOVE_USER_FROM_TENANT; {userPrincipalId}";
        public const string RemoveUserFromTenantResult = "REMOVE_USER_FROM_TENANT_RESULT; {@response}; {content}";

        public static readonly Dictionary<TelemetryEvent, string> TelemetryEventsMap = new[] { TelemetryEvent.FreeTrialClick, TelemetryEvent.IbizaClick, TelemetryEvent.MonacoClick }.Zip(new [] {FreeTrialClick, IbizaClick, MonacoClick}, (a, b) => new { Key = a, Value = b }).ToDictionary(a => a.Key, b => b.Value);

    }
}
