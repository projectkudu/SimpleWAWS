using ARMClient.Library;
using Newtonsoft.Json.Linq;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        private static readonly ARMLib csmClient;
        private static readonly ARMLib graphClient;

        private const string _readerRole = "acdd72a7-3385-48ef-bd42-f606fba81ae7";
        private const string _contributorRold = "b24988ac-6180-42a0-ab88-20f7382dd24c";

        private static readonly AsyncLock _rbacLock = new AsyncLock();

        static CsmManager()
        {
            Func<string, string> config = (s) => ConfigurationManager.AppSettings[s] ?? Environment.GetEnvironmentVariable(s);
            csmClient = ARMLib.GetDynamicClient(apiVersion: "", retryCount: 3)
                .ConfigureLogin(LoginType.Upn, config("TryUserName"), config("TryPassword"));

            graphClient = ARMLib.GetDynamicClient(apiVersion: "", retryCount: 3)
                .ConfigureLogin(LoginType.Upn, config("grapAndCsmUserName"), config("graphAndCsmPassword"));
        }

        public static async Task<string> GetUserObjectId(string puidOrAltSec, string emailAddress)
        {

            if (string.IsNullOrEmpty(puidOrAltSec) ||
                string.IsNullOrEmpty(emailAddress) ||
                puidOrAltSec.IndexOf("live.com", StringComparison.OrdinalIgnoreCase) == -1)
            {
                SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.NoRbacAccess, puidOrAltSec, emailAddress);
                return null;
            }

            var rbacUser = new RbacUser
            {
                TenantId = ConfigurationManager.AppSettings["tryWebsitesTenantId"],
                UserPuid = puidOrAltSec.Split(':').Last()
            };

            try
            {
                var users = await SearchGraph(rbacUser);

                if (users.value.Count() == 0)
                {
                    var invitation = new
                    {
                        creationType = "Invitation",
                        displayName = emailAddress,
                        primarySMTPAddress = emailAddress,
                        userType = "Guest"
                    };

                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.InviteUser, rbacUser);
                    //invite user
                    var graphResponse = await graphClient.HttpInvoke(HttpMethod.Post, CsmTemplates.GraphUsers.Bind(rbacUser), invitation);

                    graphResponse.EnsureSuccessStatusCode();
                    var invite = await graphResponse.Content.ReadAsAsync<JObject>();

                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.RedeemUserInvitation);
                    //redeem invite on user's behalf
                    graphResponse = await graphClient.HttpInvoke(HttpMethod.Post, CsmTemplates.GraphRedeemInvite.Bind(rbacUser), new
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
                    graphResponse.EnsureSuccessStatusCode();
                    var redemption = await graphResponse.Content.ReadAsAsync<JObject>();
                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.UserAddedToTenant, redemption["objectId"].ToString());
                    return redemption["objectId"].ToString();
                }
                else
                {
                    SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.UserAlreadyInTenant, users.value.First().objectId);
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
                var rbacRole = csmResource is ServerFarm
                ? _readerRole
                : _contributorRold;
                // add rbac contributor
                // after adding a user, CSM can't find the user for a while
                // pulling on the Graph GET doesn't work because that would return 200 while CSM still doesn't recognize the new user
                for (var i = 0; i < 30; i++)
                {
                    var csmResponse = await csmClient.HttpInvoke(HttpMethod.Put,
                        new Uri(string.Concat(CsmTemplates.CsmRootUrl, csmResource.CsmId, "/providers/Microsoft.Authorization/RoleAssignments/", Guid.NewGuid().ToString(), "?api-version=", ConfigurationManager.AppSettings["rbacCsmApiVersion"])),
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
                        csmResponse.EnsureSuccessStatusCode();
                        return true;
                    }
                }
            }
            catch(Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, AnalyticsEvents.ErrorInAddRbacUser, objectId);
            }

            SimpleTrace.Diagnostics.Verbose(AnalyticsEvents.FailedToAddRbacAccess);
            return false;
        }

        private static async Task<GraphArrayWrapper<GraphUser>> SearchGraph(RbacUser rbacUser)
        {
            var graphResponse = await graphClient.HttpInvoke(HttpMethod.Get, CsmTemplates.GraphSearchUsers.Bind(rbacUser));
            graphResponse.EnsureSuccessStatusCode();
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
                        throw new ArgumentException(string.Format("Couldn't convert PUID '{0}' as hex string to byte array.", puid));
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
    }
}
