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

        public static readonly CsmTemplate Subscriptions = new CsmTemplate("https://management.azure.com/subscriptions", csmApiVersoin);
        public static readonly CsmTemplate Subscription = new CsmTemplate(Subscriptions.TemplateUrl + "/{subscriptionId}", csmApiVersoin);

        public static readonly CsmTemplate ResourceGroups = new CsmTemplate(Subscription.TemplateUrl + "/resourceGroups", csmApiVersoin);
        public static readonly CsmTemplate ResourceGroup = new CsmTemplate(ResourceGroups.TemplateUrl + "/{resourceGroupName}", csmApiVersoin);

        public static readonly CsmTemplate DeployCsmTemplate = new CsmTemplate(ResourceGroup.TemplateUrl + "/deployments/{deploymentName}", csmApiVersoin);

        public static readonly CsmTemplate AppServiceRegister = new CsmTemplate(Subscription.TemplateUrl + "/providers/Microsoft.AppService/register", appServiceApiVersion);
        public static readonly CsmTemplate AppServiceGenerateCsmDeployTemplate = new CsmTemplate(Subscription.TemplateUrl + "/providers/Microsoft.AppService/deploymenttemplates/{appServiceName}/generate?resourceGroup={resourceGroupName}", appServiceApiVersion);

        public static readonly CsmTemplate Sites = new CsmTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/sites", websitesApiVersion);
        public static readonly CsmTemplate Site = new CsmTemplate(Sites.TemplateUrl + "/{siteName}", websitesApiVersion);
        public static readonly CsmTemplate GetSiteAppSettings= new CsmTemplate(Site.TemplateUrl + "/Config/AppSettings/list", websitesApiVersion);
        public static readonly CsmTemplate PutSiteAppSettings= new CsmTemplate(Site.TemplateUrl + "/Config/AppSettings", websitesApiVersion);
        public static readonly CsmTemplate GetSiteMetadata= new CsmTemplate(Site.TemplateUrl + "/Config/Metadata/list", websitesApiVersion);
        public static readonly CsmTemplate PutSiteMetadata= new CsmTemplate(Site.TemplateUrl + "/Config/Metadata", websitesApiVersion);
        public static readonly CsmTemplate SiteConfig= new CsmTemplate(Site.TemplateUrl + "/Config/web", websitesApiVersion);
        public static readonly CsmTemplate SitePublishingProfile= new CsmTemplate(Site.TemplateUrl + "/publishxml", websitesApiVersion);
    }
}