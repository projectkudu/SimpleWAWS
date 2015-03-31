using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{

    public class CsmSite
    {
        public string name { get; set; }
        public string state { get; set; }
        public string[] hostNames { get; set; }
        public string webSpace { get; set; }
        public string selfLink { get; set; }
        public string repositorySiteName { get; set; }
        public object owner { get; set; }
        public int usageState { get; set; }
        public bool enabled { get; set; }
        public bool adminEnabled { get; set; }
        public string[] enabledHostNames { get; set; }
        public Siteproperties siteProperties { get; set; }
        public int availabilityState { get; set; }
        public object sslCertificates { get; set; }
        public object[] csrs { get; set; }
        public object cers { get; set; }
        public object siteMode { get; set; }
        public HostnameSslState[] hostNameSslStates { get; set; }
        public object computeMode { get; set; }
        public object serverFarm { get; set; }
        public object serverFarmId { get; set; }
        public DateTime lastModifiedTimeUtc { get; set; }
        public string storageRecoveryDefaultState { get; set; }
        public int contentAvailabilityState { get; set; }
        public int runtimeAvailabilityState { get; set; }
        public object siteConfig { get; set; }
        public string deploymentId { get; set; }
        public object trafficManagerHostNames { get; set; }
        public string sku { get; set; }
        public object premiumAppDeployed { get; set; }
        public bool scmSiteAlsoStopped { get; set; }
        public object targetSwapSlot { get; set; }
        public object hostingEnvironment { get; set; }
        public string microService { get; set; }
        public object gatewaySiteName { get; set; }
        public object kind { get; set; }
        public object cloningInfo { get; set; }
    }

    public class Siteproperties
    {
        public object metadata { get; set; }
        public object[] properties { get; set; }
        public object appSettings { get; set; }
    }

    public class HostnameSslState
    {
        public string name { get; set; }
        public int sslState { get; set; }
        public object ipBasedSslResult { get; set; }
        public object virtualIP { get; set; }
        public object thumbprint { get; set; }
        public object toUpdate { get; set; }
        public object toUpdateIpBasedSsl { get; set; }
        public int ipBasedSslState { get; set; }
        public int hostType { get; set; }
    }
}