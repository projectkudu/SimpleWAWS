using System;
using ARMClient.Library;

namespace SimpleWAWS.Code
{
    public static class ArmUriTemplates
    {
        private const string csmApiVersion = "2014-04-01";
        private const string websitesApiVersion = "2014-06-01";
        private const string websitesCreateApiVersion = "2016-08-01";
        //private const string websitesServerFarmCreateApiVersion = "2016-09-01";
        private const string functionsApiVersion = "2016-08-01";
        private const string newWebsitesApiVersion = "2015-02-01";
        private const string appServiceApiVersion = "2015-03-01-preview";
        private const string linuxResourceApiVersion = "2016-08-01";
        private const string vscodelinuxResourceApiVersion = "2016-08-01";
        private const string graphApiVersion = "1.42-previewInternal";
        private const string storageApiVersion = "2015-05-01-preview";
        private const string csmTemplateApiVersion = "2017-05-10";

        public const string RbacApiVersion = "2014-07-01-preview";
        public const string CsmRootUrl = "https://management.azure.com";
        public const string GraphRootUrl = "https://graph.windows.net";
        public static string TryAppServiceUrl = SimpleSettings.TryAppServiceSite + "/api/resource/all/true";

        public static readonly Uri LoadedResources = new Uri(TryAppServiceUrl);
        public static readonly ArmUriTemplate Subscriptions = new ArmUriTemplate(CsmRootUrl + "/subscriptions", csmApiVersion);
        public static readonly ArmUriTemplate Subscription = new ArmUriTemplate(Subscriptions.TemplateUrl + "/{subscriptionId}", csmApiVersion);
        public static readonly ArmUriTemplate SubscriptionResources = new ArmUriTemplate(Subscription.TemplateUrl + "/resources", csmApiVersion);

        public static readonly ArmUriTemplate ResourceGroups = new ArmUriTemplate(Subscription.TemplateUrl + "/resourceGroups", csmApiVersion);
        public static readonly ArmUriTemplate ResourceGroup = new ArmUriTemplate(ResourceGroups.TemplateUrl + "/{resourceGroupName}", csmApiVersion);
        public static readonly ArmUriTemplate ResourceGroupResources = new ArmUriTemplate(ResourceGroup.TemplateUrl + "/resources", csmApiVersion);

        public static readonly ArmUriTemplate CsmTemplateDeployment = new ArmUriTemplate(ResourceGroup.TemplateUrl + "/deployments/{deploymentName}", csmTemplateApiVersion);

        public static readonly ArmUriTemplate WebsitesRegister = new ArmUriTemplate(Subscription.TemplateUrl + "/providers/Microsoft.Web/register", websitesApiVersion);
        public static readonly ArmUriTemplate Sites = new ArmUriTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/sites", websitesApiVersion);
        public static readonly ArmUriTemplate Site = new ArmUriTemplate(Sites.TemplateUrl + "/{siteName}", websitesApiVersion);
        public static readonly ArmUriTemplate SiteCreate = new ArmUriTemplate(Sites.TemplateUrl + "/{siteName}", websitesCreateApiVersion);
        public static readonly ArmUriTemplate SiteStart = new ArmUriTemplate(Sites.TemplateUrl + "/{siteName}/start", websitesCreateApiVersion);
        public static readonly ArmUriTemplate SiteStop = new ArmUriTemplate(Sites.TemplateUrl + "/{siteName}/stop", websitesCreateApiVersion);
        public static readonly ArmUriTemplate FunctionsAppApiVersionTemplate = new ArmUriTemplate(Sites.TemplateUrl + "/{siteName}", functionsApiVersion);
        public static readonly ArmUriTemplate GetSiteAppSettings = new ArmUriTemplate(Site.TemplateUrl + "/config/AppSettings/list", websitesApiVersion);
        public static readonly ArmUriTemplate PutSiteAppSettings = new ArmUriTemplate(Site.TemplateUrl + "/config/AppSettings", websitesApiVersion);
        public static readonly ArmUriTemplate GetSiteMetadata = new ArmUriTemplate(Site.TemplateUrl + "/config/Metadata/list", websitesApiVersion);
        public static readonly ArmUriTemplate PutSiteMetadata = new ArmUriTemplate(Site.TemplateUrl + "/config/Metadata", websitesApiVersion);
        public static readonly ArmUriTemplate SiteConfig = new ArmUriTemplate(Site.TemplateUrl + "/config/web", websitesApiVersion);
        public static readonly ArmUriTemplate SitePublishingCredentials = new ArmUriTemplate(Site.TemplateUrl + "/config/PublishingCredentials/list", websitesApiVersion);
        public static readonly ArmUriTemplate SitePublishingProfile = new ArmUriTemplate(Site.TemplateUrl + "/publishxml", websitesApiVersion);
        public static readonly ArmUriTemplate SiteDeployments = new ArmUriTemplate(Site.TemplateUrl + "/deployments", newWebsitesApiVersion);

        //public static readonly ArmUriTemplate ServerFarms = new ArmUriTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/serverFarms", websitesApiVersion);
        //public static readonly ArmUriTemplate ServerFarm = new ArmUriTemplate(ServerFarms.TemplateUrl + "/{serverFarmName}", websitesApiVersion);
        //public static readonly ArmUriTemplate ServerFarmCreate = new ArmUriTemplate(ServerFarms.TemplateUrl + "/{serverFarmName}", websitesServerFarmCreateApiVersion);

        public static readonly ArmUriTemplate GraphTenant = new ArmUriTemplate(GraphRootUrl + "/{tenantId}", graphApiVersion);
        public static readonly ArmUriTemplate GraphUsers = new ArmUriTemplate(GraphTenant.TemplateUrl + "/users", graphApiVersion);
        public static readonly ArmUriTemplate GraphUser = new ArmUriTemplate(GraphUsers.TemplateUrl + "/{userId}", graphApiVersion);
        public static readonly ArmUriTemplate GraphSearchUsers = new ArmUriTemplate(GraphUsers.TemplateUrl + "/?$filter=netId eq '{userPuid}' or alternativeSecurityIds/any(x:x/type eq 1 and x/identityProvider eq null and x/key eq X'{userPuid}')", graphApiVersion);
        public static readonly ArmUriTemplate GraphRedeemInvite = new ArmUriTemplate(GraphTenant.TemplateUrl + "/redeemInvitation", graphApiVersion);

        public static readonly ArmUriTemplate StorageRegister = new ArmUriTemplate(Subscription.TemplateUrl + "/providers/Microsoft.Storage/register", storageApiVersion);
        public static readonly ArmUriTemplate StorageAccounts = new ArmUriTemplate(ResourceGroup.TemplateUrl+ "/providers/Microsoft.Storage/storageAccounts", storageApiVersion);
        public static readonly ArmUriTemplate StorageAccount = new ArmUriTemplate(StorageAccounts.TemplateUrl + "/{storageAccountName}", storageApiVersion);
        public static readonly ArmUriTemplate StorageListKeys = new ArmUriTemplate(StorageAccount.TemplateUrl + "/listKeys", storageApiVersion);

        public static readonly ArmUriTemplate LinuxResource = new ArmUriTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/sites/{siteName}", linuxResourceApiVersion);
        public static readonly ArmUriTemplate VSCodeLinuxResource = new ArmUriTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/sites/{siteName}", vscodelinuxResourceApiVersion);
    }
}