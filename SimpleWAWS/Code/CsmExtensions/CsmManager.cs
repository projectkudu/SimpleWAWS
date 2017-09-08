using ARMClient.Library;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        private static readonly AzureClient csmClient;
        private static readonly AzureClient graphClient;
        private static readonly AzureClient linuxClient;
        private static readonly AzureClient monitoringToolsClient;
        private static readonly AzureClient monitoringToolsGraphClient;

        private const string _readerRole = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
        private const string _contributorRole = "b24988ac-6180-42a0-ab88-20f7382dd24c";

        private static readonly AsyncLock _rbacLock = new AsyncLock();
        private static IEnumerable<string> _subscriptions;
        private static Hashtable georegionHashMap = new Hashtable();
        static CsmManager()
        {
            csmClient = new AzureClient(retryCount: 3);
            csmClient.ConfigureSpnLogin(SimpleSettings.TryTenantId, SimpleSettings.TryUserName, SimpleSettings.TryPassword);

            graphClient = new AzureClient(retryCount: 3);
            graphClient.ConfigureUpnLogin(SimpleSettings.GraphUserName, SimpleSettings.GraphPassword);

            linuxClient = new AzureClient(retryCount: 3);
            linuxClient.ConfigureSpnLogin(SimpleSettings.LinuxTenantId, SimpleSettings.LinuxServicePrincipal, SimpleSettings.LinuxServicePrincipalKey);

            monitoringToolsClient = new AzureClient(retryCount: 3);
            monitoringToolsClient.ConfigureSpnLogin(SimpleSettings.MonitoringToolsTenantId, SimpleSettings.MonitoringToolsServicePrincipal, SimpleSettings.MonitoringToolsServicePrincipalKey);

            monitoringToolsGraphClient = new AzureClient(retryCount: 3);
            monitoringToolsGraphClient.ConfigureUpnLogin(SimpleSettings.MonitoringToolsGraphUserName, SimpleSettings.MonitoringToolsGraphPassword);
        }
        public static string LoadMonitoringToolsSubscription()
        {
            return SimpleSettings.MonitoringToolsSubscription;
        }
        public static async Task<IEnumerable<string>> GetSubscriptions()
        {
            if (_subscriptions == null)
            {
                // Load all subscriptions
                var csmSubscriptions = await CsmManager.GetSubscriptionNamesToIdMap();
                _subscriptions = (SimpleSettings.Subscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Concat(SimpleSettings.LinuxSubscriptions.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)))
                    //It can be either a displayName or a subscriptionId
                    //For Linux it needs to be subscriptionId
                    .Select(s => s.Trim())
                    .Where(n =>
                    {
                        Guid temp;
                        return csmSubscriptions.ContainsKey(n) || Guid.TryParse(n, out temp);
                    })
                    .Select(sn =>
                    {
                        Guid temp;
                        if (Guid.TryParse(sn, out temp)) return sn;
                        else return csmSubscriptions[sn];
                    });
            }
            return _subscriptions;
        }

        public static Hashtable RegionHashTable
        {
            get
            {
                if (georegionHashMap.Count ==0)
                {
                    foreach (var a in SimpleSettings.GeoRegions.Split(','))
                    {
                        georegionHashMap.Add(a.ToLowerInvariant().Replace(" ", ""), a);
                    }
                }
                return georegionHashMap;
            }
        }
        static AzureClient GetGraphClient(SubscriptionType subscriptionType) {
            switch (subscriptionType)
            {
                case SubscriptionType.MonitoringTools:
                    return monitoringToolsGraphClient;
                case SubscriptionType.Linux:
                case SubscriptionType.AppService:
                default:
                    return graphClient;
            }
        }
        static AzureClient GetClient(SubscriptionType subscriptionType)
        {
            switch (subscriptionType)
            {
                case SubscriptionType.Linux:
                            return linuxClient;
                case SubscriptionType.MonitoringTools:
                    return monitoringToolsClient;
                case SubscriptionType.AppService:
                    default:
                            return csmClient;
            }
        }

        public static async Task<string> GetUserObjectId(string puidOrAltSec, string emailAddress, ResourceGroup resourceGroup)
        {
            if (string.IsNullOrEmpty(puidOrAltSec) ||
                string.IsNullOrEmpty(emailAddress) ||
                !SimpleWAWS.Authentication.AADProvider.IsMSA(puidOrAltSec))
            {
                SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.NoRbacAccess);
                return null;
            }

            var rbacUser = new RbacUser
            {
                TenantId = resourceGroup.TenantName,
                UserPuid = puidOrAltSec.Split(':').Last()
            };

            try
            {
                var users = await SearchGraph(rbacUser, resourceGroup);

                if (!users.value.Any())
                {
                    var invitation = new
                    {
                        creationType = "Invitation",
                        displayName = emailAddress,
                        primarySMTPAddress = emailAddress,
                        userType = "Guest"
                    };

                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.InviteUser);
                    //invite user
                    var graphResponse = await GetGraphClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.GraphUsers.Bind(rbacUser), invitation);
                    await graphResponse.EnsureSuccessStatusCodeWithFullError();
                    var invite = await graphResponse.Content.ReadAsAsync<JObject>();

                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.RedeemUserInvitation);
                    //redeem invite on user's behalf
                    graphResponse = await GetGraphClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Post, ArmUriTemplates.GraphRedeemInvite.Bind(rbacUser), new
                    {
                        altSecIds = new[]{ new {
                            identityProvider = (string)null,
                            type = "1", // for MSA
                            key = GetEncodedPuid(rbacUser.UserPuid)
                        }},
                        acceptedAs = emailAddress,
                        inviteTicket = new
                        {
                            Ticket = invite["inviteTicket"][0]["Ticket"],
                            Type = invite["inviteTicket"][0]["Type"]
                        }
                    });
                    await graphResponse.EnsureSuccessStatusCodeWithFullError();
                    var redemption = await graphResponse.Content.ReadAsAsync<JObject>();
                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.UserAddedToTenant);
                    return redemption["objectId"].ToString();
                }
                else
                {
                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.UserAlreadyInTenant);
                    return users.value.First().objectId;
                }
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, AnalyticsEvents.ErrorInAddRbacUser, new { Puid = puidOrAltSec, Email = emailAddress });
            }
            return null;
        }

        public static async Task<bool> AddRbacAccess(this BaseResource csmResource, string objectId)
        {
            try
            {
                var rbacRole = csmResource.SubscriptionType == SubscriptionType.MonitoringTools|| csmResource is ServerFarm || csmResource is ResourceGroup
                ? _readerRole
                : _contributorRole;
                // add rbac contributor
                // after adding a user, CSM can't find the user for a while
                // pulling on the Graph GET doesn't work because that would return 200 while CSM still doesn't recognize the new user
                for (var i = 0; i < 30; i++)
                {
                    var csmResponse = await GetClient(csmResource.SubscriptionType).HttpInvoke(HttpMethod.Put,
                        new Uri(string.Concat(ArmUriTemplates.CsmRootUrl, csmResource.CsmId, "/providers/Microsoft.Authorization/RoleAssignments/", Guid.NewGuid().ToString(), "?api-version=", ArmUriTemplates.RbacApiVersion)),
                        new
                        {
                            properties = new
                            {
                                roleDefinitionId = string.Concat("/subscriptions/", csmResource.SubscriptionId, "/providers/Microsoft.Authorization/roleDefinitions/", rbacRole),
                                principalId = objectId
                            }
                        });
                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.AssignRbacResult, csmResponse.StatusCode);
                    if (csmResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        await Task.Delay(1000);
                    }
                    else
                    {
                        await csmResponse.EnsureSuccessStatusCodeWithFullError();
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, AnalyticsEvents.ErrorInAddRbacUser);
            }

            SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.FailedToAddRbacAccess);
            return false;
        }

        public static async Task<Dictionary<string, string>> GetSubscriptionNamesToIdMap()
        {
            var response = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.Subscriptions.Bind(""));
            await response.EnsureSuccessStatusCodeWithFullError();

            var appServiceSubscriptions = await response.Content.ReadAsAsync<CsmSubscriptionsArray>();

            response = await linuxClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.Subscriptions.Bind(""));
            await response.EnsureSuccessStatusCodeWithFullError();

            var linuxSubscriptions = await response.Content.ReadAsAsync<CsmSubscriptionsArray>();

            return (appServiceSubscriptions.value.Union(linuxSubscriptions.value)).GroupBy(sub => sub.subscriptionId)
                   .Select(group => group.First()).ToDictionary(k => k.displayName, v => v.subscriptionId);
        }

        private static async Task<GraphArrayWrapper<GraphUser>> SearchGraph(RbacUser rbacUser, ResourceGroup resourceGroup)
        {
            var graphResponse = await GetGraphClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.GraphSearchUsers.Bind(rbacUser));
            await graphResponse.EnsureSuccessStatusCodeWithFullError();
            return await graphResponse.Content.ReadAsAsync<GraphArrayWrapper<GraphUser>>();
        }

        private static string GetEncodedPuid(string puid)
        {
            return Convert.ToBase64String(EncodePuidToBytes(puid));
        }

        private static byte[] EncodePuidToBytes(string puid)
        {
            // Convert each pair of hex digits in PUID to byte in byte array
            return puid
                .BatchEnumerable(2)
                .Select(characterPair =>
                {
                    byte currentByte;
                    if (!byte.TryParse(
                        s: new string(characterPair.ToArray()),
                        style: NumberStyles.HexNumber,
                        provider: CultureInfo.InvariantCulture,
                        result: out currentByte))
                    {
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Couldn't convert PUID '{0}' as hex string to byte array.", puid));
                    }

                    return currentByte;
                }).ToArray();
        }

        private static IEnumerable<IEnumerable<T>> BatchEnumerable<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            if (batch.Any())
            {
                yield return batch;
            }
        }
        //public static async Task<JObject> GetLoadedResources()
        //{

        //    var tryAppServiceResponse = await GetGraphClient(resourceGroup.SubscriptionType).HttpInvoke(HttpMethod.Get, ArmUriTemplates.LoadedResources);
        //    await tryAppServiceResponse.EnsureSuccessStatusCodeWithFullError();
        //    return await tryAppServiceResponse.Content.ReadAsAsync<JObject>();
        //}
    }

}
