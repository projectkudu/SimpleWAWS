using System.Linq;
using System.Collections.Generic;
using SimpleWAWS.Code;

namespace SimpleWAWS.Models
{
    public static class AnalyticsEvents
    {
        // Event format "USER_LOGGED_IN; {userName}"
        public const string UserLoggedIn = "USER_LOGGED_IN; {@user}";
        public const string OldUserLoggedIn = "USER_LOGGED_IN";

        public const string UserGotError = "USER_GOT_ERROR; {user}; {errorMessage}; Count: {Count}";
        public const string MoreThanOneError = "MORE_THAN_ONE_ERROR; {user}; Count: {Count}";

        // Event format "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE; {userName}; {language}; {template}; {siteUniqueId}"
        public const string UserCreatedSiteWithLanguageAndTemplateName = "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE; {@user}; {@template}; {resourceGroupId}";
        public const string OldUserCreatedSiteWithLanguageAndTemplateName = "USER_CREATED_SITE_WITH_LANGUAGE_AND_TEMPLATE";

        public const string UserPuidValue = "USER_PUID_VALUE; {@user}";
        public const string ErrorInRemoveRbacUser = "ERROR_REMOVE_RBAC_USER; {resourceGroupId}";
        public const string ErrorInAddRbacUser = "ERROR_ADD_RBAC_USER; {@user} Count: {Count}";
        public const string ErrorInCheckRbacUser = "ERROR_CHECK_RBAC_USER; {resourceGroupId}";

        public const string RemoveUserFromTenant = "REMOVE_USER_FROM_TENANT; {userPrincipalId}";
        public const string RemoveUserFromTenantResult = "REMOVE_USER_FROM_TENANT_RESULT; {@response}; {content}";

        public const string UiEvent = "UI_EVENT; {eventName}; {@properties}";
        public const string OldUiEvent = "UI_EVENT";

        public const string NoRbacAccess = "NO_RBAC_ACCESS; {puid}; {email}";
        public const string SearchGraphForUser = "SEARCH_GRAPH_FOR_USER; {@rbacUser}";
        public const string SearchGraphResponse = "SEARCH_GRAPH_RESPONSE; {@response}";
        public const string InviteUser = "INVITE_USER; {@rbacUser}";
        public const string RedeemUserInvitation = "REDEEM_USER_INVITATION";
        public const string UserAlreadyInTenant = "USER_ALREADY_IN_TENANT; {objectId}";
        public const string AssignRbacResult = "ASSIGN_RBAC_RESULT; {csmResponseStatusCode}";
        public const string FailedToAddRbacAccess = "FAILED_TO_ADD_RBAC_ACCESS";
        public const string UserAddedToTenant = "USER_ADDED_TO_TENANT; {objectId}";

        public const string AnonymousUserCreated = "ANONYMOUS_USER_CREATED";
        public const string AnonymousUserLogedIn = "ANONYMOUS_USER_LOGGEDIN";
        public const string AnonymousUserInit = "ANONYMOUS_USER_INIT";

        public const string FeedbackComment = "FEEDBACK_COMMENT";
        public const string ExtendTrial = "EXTEND_TRIAL";
    }
}
