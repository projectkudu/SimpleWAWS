using ARMClient.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class CsmTemplates
    {
        private const string defaultCsmApiVersoin = "2014-04-01";
        private const string defaultWebsitesApiVersion = "2014-06-01";

        public static readonly CsmTemplate Subscriptions = new CsmTemplate("https://management.azure.com/subscriptions", defaultCsmApiVersoin);
        public static readonly CsmTemplate Subscription = new CsmTemplate(Subscriptions.TemplateUrl + "/{subscriptionId}", defaultCsmApiVersoin);

        public static readonly CsmTemplate ResourceGroups = new CsmTemplate(Subscription.TemplateUrl + "/resourceGroups", defaultCsmApiVersoin);
        public static readonly CsmTemplate ResourceGroup = new CsmTemplate(ResourceGroups.TemplateUrl + "/{resourceGroupName}", defaultCsmApiVersoin);

        public static readonly CsmTemplate Sites = new CsmTemplate(ResourceGroup.TemplateUrl + "/providers/Microsoft.Web/sites", defaultWebsitesApiVersion);
        public static readonly CsmTemplate Site = new CsmTemplate(Sites.TemplateUrl + "/{siteName}", defaultWebsitesApiVersion);
        public static readonly CsmTemplate GetSiteAppSettings= new CsmTemplate(Site.TemplateUrl + "/Config/AppSettings/list", defaultWebsitesApiVersion);
        public static readonly CsmTemplate PutSiteAppSettings= new CsmTemplate(Site.TemplateUrl + "/Config/AppSettings", defaultWebsitesApiVersion);
        public static readonly CsmTemplate GetSiteMetadata= new CsmTemplate(Site.TemplateUrl + "/Config/Metadata/list", defaultWebsitesApiVersion);
        public static readonly CsmTemplate PutSiteMetadata= new CsmTemplate(Site.TemplateUrl + "/Config/Metadata", defaultWebsitesApiVersion);
        public static readonly CsmTemplate SiteConfig= new CsmTemplate(Site.TemplateUrl + "/Config/web", defaultWebsitesApiVersion);
        public static readonly CsmTemplate SitePublishingProfile= new CsmTemplate(Site.TemplateUrl + "/publishxml", defaultWebsitesApiVersion);
    }
}