using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class Constants
    {
        public const string StartTime = "StartTime";
        public const string IsRbacEnabled = "IsRbacEnabled";
        public const string GeoRegion = "GeoRegion";
        public const string UserId = "UserId";
        public const string CommonApiAppsDeployed = "CommonApiAppsDeployed";
        public const string CommonApiAppsDeployedVersion = "1.0.0";
        public const string TryResourceGroupPrefix = "TRY_RG";
        public const string TryResourceGroupSeparator = "_";
        public const string LifeTimeInMinutes = "LifeTimeInMinutes";
        public const string AppService = "AppService";
        public const string TiPCookie = "x-ms-routing-name";
        public const string TemplateName = "TemplateName";
        public const string IsExtended = "IsExtended";
        public const string FunctionsSitePrefix = "Functions";
        public const string FunctionsContainerDeployed = "FunctionsContainerDeployed";
        public const string FunctionsContainerDeployedVersion = "3.0.0";
        public const string FunctionsStorageAccountPrefix = "Functions";
        public const string AzureStorageAppSettingsName = "AzureWebJobsStorage";
        public const string AzureStorageDashboardAppSettingsName = "AzureWebJobsDashboard";
        public const string StorageConnectionStringTemplate = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}";
    }
}