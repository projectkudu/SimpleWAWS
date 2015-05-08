using ARMClient.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class CsmTemplates
    {
        private const string csmApiVersoin = "2014-04-01";
        private const string websitesApiVersion = "2014-06-01";
        private const string appServiceApiVersion = "2015-03-01-preview";
        private const string graphApiVersion = "1.42-previewInternal";

        public const string CsmRootUrl = "https://management.azure.com";

        public static readonly CsmTemplate Subscriptions = new CsmTemplate(CsmRootUrl + "/subscriptions", csmApiVersoin);
        public static readonly CsmTemplate Subscription = new CsmTemplate(Subscriptions.TemplateUrl + "/{subscriptionId}", csmApiVersoin);

        public static readonly CsmTemplate ResourceGroups = new CsmTemplate(Subscription.TemplateUrl + "/resourceGroups", csmApiVersoin);
        public static readonly CsmTemplate ResourceGroup = new CsmTemplate(ResourceGroups.TemplateUrl + "/{resourceGroupName}", csmApiVersoin);

        public static readonly CsmTemplate CsmTemplateDeployment = new CsmTemplate(ResourceGroup.TemplateUrl + "/deployments/{deploymentName}", csmApiVersoin);

        public static readonly CsmTemplate AppServiceRegister = new CsmTemplate(Subscription.TemplateUrl + "/providers/Microsoft.AppService/register", appServiceApiVersion);
        public static readonly CsmTemplate AppServiceGenerateCsmDeployTemplate = new CsmTemplate(Subscription.TemplateUrl + "/providers/Microsoft.AppService/deploymenttemplates/{microserviceId}/generate?resourceGroup={resourceGroupName}", appServiceApiVersion);

        public static readonly CsmTemplate Sites = new CsmTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/sites", websitesApiVersion);
        public static readonly CsmTemplate Site = new CsmTemplate(Sites.TemplateUrl + "/{siteName}", websitesApiVersion);
        public static readonly CsmTemplate GetSiteAppSettings = new CsmTemplate(Site.TemplateUrl + "/config/AppSettings/list", websitesApiVersion);
        public static readonly CsmTemplate PutSiteAppSettings = new CsmTemplate(Site.TemplateUrl + "/config/AppSettings", websitesApiVersion);
        public static readonly CsmTemplate GetSiteMetadata = new CsmTemplate(Site.TemplateUrl + "/config/Metadata/list", websitesApiVersion);
        public static readonly CsmTemplate PutSiteMetadata = new CsmTemplate(Site.TemplateUrl + "/config/Metadata", websitesApiVersion);
        public static readonly CsmTemplate SiteConfig = new CsmTemplate(Site.TemplateUrl + "/config/web", websitesApiVersion);
        public static readonly CsmTemplate SitePublishingCredentials = new CsmTemplate(Site.TemplateUrl + "/config/PublishingCredentials/list", websitesApiVersion);
        public static readonly CsmTemplate SitePublishingProfile = new CsmTemplate(Site.TemplateUrl + "/publishxml", websitesApiVersion);

        public static readonly CsmTemplate ApiApps = new CsmTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.AppService/apiapps", appServiceApiVersion);
        public static readonly CsmTemplate ApiApp = new CsmTemplate(ApiApps.TemplateUrl + "/{apiAppName}", appServiceApiVersion);

        public static readonly CsmTemplate Gateways = new CsmTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.AppService/gateways", appServiceApiVersion);
        public static readonly CsmTemplate Gateway = new CsmTemplate(Gateways.TemplateUrl + "/{gatewayName}", appServiceApiVersion);

        public static readonly CsmTemplate ServerFarms = new CsmTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/serverFarms", websitesApiVersion);
        public static readonly CsmTemplate ServerFarm = new CsmTemplate(ServerFarms.TemplateUrl + "/{serverFarmName}", websitesApiVersion);

        public static readonly CsmTemplate GraphTenant = new CsmTemplate("https://graph.windows.net/{tenantId}", graphApiVersion);
        public static readonly CsmTemplate GraphUsers = new CsmTemplate(GraphTenant.TemplateUrl + "/users", graphApiVersion);
        public static readonly CsmTemplate GraphUser = new CsmTemplate(GraphUsers.TemplateUrl + "/{userId}", graphApiVersion);
        public static readonly CsmTemplate GraphSearchUsers = new CsmTemplate(GraphUsers.TemplateUrl + "/?$filter=netId eq '{userPuid}' or alternativeSecurityIds/any(x:x/type eq 1 and x/identityProvider eq null and x/key eq X'{userPuid}')", graphApiVersion);
        public static readonly CsmTemplate GraphRedeemInvite = new CsmTemplate(GraphTenant.TemplateUrl + "/redeemInvitation", graphApiVersion);
    }
}