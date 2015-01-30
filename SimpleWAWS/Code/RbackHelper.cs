using ARMClient.Authentication.AADAuthentication;
using ARMClient.Library;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class RbackHelper
    {
        public static async Task<bool> AddRbacUser(string puidOrAltSec, string emailAddress, Site site)
        {
            var puid = puidOrAltSec.Split(':').Last();
            try
            {
                var graphClient = ARMLib.GetDynamicClient(apiVersion: "1.42-previewInternal", url: "https://graph.windows.net/590d33be-69d3-46c7-b4f8-02a61d7658af")
                    .ConfigureLogin(LoginType.Upn, "userName", "password");

                var csmClient = ARMLib.GetDynamicClient(apiVersion: "2014-07-01-preview")
                    .ConfigureLogin(LoginType.Upn, "userName", "password");

                //check if user is already in directory
                var user = await graphClient.Users.Query("$filter=netId eq '" + puid + "' or alternativeSecurityIds/any(x:x/type eq 1 and x/identityProvider eq null and x/key eq X'" + puid + "')").GetAsync<JObject>();
                string oid = null;
                if (user.value.Count == 0)
                {
                    // invite user
                    var invite = await graphClient.Users.PostAsync<JObject>(new
                    {
                        creationType = "Invitation",
                        displayName = emailAddress,
                        primarySMTPAddress = emailAddress,
                        userType = "Guest"
                    });

                    // redeem invite
                    var redemption = await graphClient.RedeemInvitation.PostAsync<JObject>(new
                    {
                        altSecIds = new[]{ new {
                            identityProvider = (string)null,
                            type = "1", // for MSA
                            key = GetEncodedPuid(puid)
                        }},
                        acceptedAs = emailAddress,
                        inviteTicket = new
                        {
                            Ticket = invite.inviteTicket[0].Ticket,
                            Type = invite.inviteTicket[0].Type
                        }
                    });
                    oid = redemption.objectId;
                }
                else
                {
                    oid = user.value[0].objectId;
                }

                // add rbac contributer
                for (int i = 0; i < 30; i++)
                {
                    var response = (HttpResponseMessage)await csmClient.Subscriptions[site.WebSpace.SubscriptionId]
                                                            .ResourceGroups[site.WebSpace.ResourceGroup]
                                                            .Providers["Microsoft.Web"]
                                                            .Sites[site.Name]
                                                            .Providers["Microsoft.Authorization"]
                                                            .RoleAssignments[site.SiteUniqueId]
                                                            .PutAsync(new
                                                            {
                                                                properties = new
                                                                {
                                                                    roleDefinitionId = "/subscriptions/" + site.WebSpace.SubscriptionId + "/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c",
                                                                    principalId = oid
                                                                }
                                                            });
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        Trace.TraceInformation(await response.Content.ReadAsStringAsync());
                        await Task.Delay(1000);
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("{0}; {1}; {2}; {3}", AnalyticsEvents.ErrorInAddRbacUser, e.GetBaseException().Message.RemoveNewLines(), puidOrAltSec, emailAddress);
                return false;
            }
            return true;
        }

        public static async Task RemoveRbacUser(Site site)
        {
            try
            {
                var graphClient = ARMLib.GetDynamicClient(apiVersion: "1.42-previewInternal", url: "https://graph.windows.net")
                                        .ConfigureLogin(LoginType.Upn, "userName", "password");

                var csmClient = ARMLib.GetDynamicClient(apiVersion: "2014-07-01-preview")
                                      .ConfigureLogin(LoginType.Upn, "userName", "password");

                //remove rbac policy
                var rbacClient = csmClient.Subscriptions[site.WebSpace.SubscriptionId]
                                          .ResourceGroups[site.WebSpace.ResourceGroup]
                                          .Providers["Microsoft.Web"]
                                          .Sites[site.Name]
                                          .Providers["Microsoft.Authorization"]
                                          .RoleAssignments[site.SiteUniqueId];

                var rbacPolicy = await rbacClient.GetAsync<JObject>();
                await rbacClient.DeleteAsync();

                //remove user from tenant
                await graphClient["590d33be-69d3-46c7-b4f8-02a61d7658af"].Users[rbacPolicy.properties.principalId].DeleteAsync();
            }
            catch(Exception e)
            {
                Trace.TraceError("{0}; {1}; {2}", AnalyticsEvents.ErrorInRemoveRbacUser, e.GetBaseException().Message.RemoveNewLines(), site.SiteUniqueId);
            }
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

        public static string RemoveNewLines(this string value)
        {
            return value.Replace("\r\n", "_").Replace('\n', '_');
        }
    }
}