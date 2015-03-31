using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{

    public class CsmSiteConfig
    {
        public int numberOfWorkers { get; set; }
        public string[] defaultDocuments { get; set; }
        public string netFrameworkVersion { get; set; }
        public string phpVersion { get; set; }
        public string pythonVersion { get; set; }
        public bool requestTracingEnabled { get; set; }
        public object requestTracingExpirationTime { get; set; }
        public bool remoteDebuggingEnabled { get; set; }
        public string remoteDebuggingVersion { get; set; }
        public bool httpLoggingEnabled { get; set; }
        public int logsDirectorySizeLimit { get; set; }
        public bool detailedErrorLoggingEnabled { get; set; }
        public string publishingUsername { get; set; }
        public object publishingPassword { get; set; }
        public object appSettings { get; set; }
        public object metadata { get; set; }
        public object connectionStrings { get; set; }
        public object handlerMappings { get; set; }
        public object documentRoot { get; set; }
        public string scmType { get; set; }
        public bool use32BitWorkerProcess { get; set; }
        public bool webSocketsEnabled { get; set; }
        public bool alwaysOn { get; set; }
        public object javaVersion { get; set; }
        public object javaContainer { get; set; }
        public object javaContainerVersion { get; set; }
        public int managedPipelineMode { get; set; }
        public Virtualapplication[] virtualApplications { get; set; }
        public int winAuthAdminState { get; set; }
        public int winAuthTenantState { get; set; }
        public bool customAppPoolIdentityAdminState { get; set; }
        public bool customAppPoolIdentityTenantState { get; set; }
        public object runtimeADUser { get; set; }
        public object runtimeADUserPassword { get; set; }
        public int loadBalancing { get; set; }
        public object[] routingRules { get; set; }
        public Experiments experiments { get; set; }
        public object limits { get; set; }
        public bool autoHealEnabled { get; set; }
        public object autoHealRules { get; set; }
        public object tracingOptions { get; set; }
        public string vnetName { get; set; }
        public bool siteAuthEnabled { get; set; }
        public object siteAuthSettings { get; set; }
        public object autoSwapSlotName { get; set; }
    }

    public class Experiments
    {
        public object[] rampUpRules { get; set; }
    }

    public class Virtualapplication
    {
        public string virtualPath { get; set; }
        public string physicalPath { get; set; }
        public bool preloadEnabled { get; set; }
        public object virtualDirectories { get; set; }
    }

}