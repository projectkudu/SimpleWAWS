using ARMClient.Authentication.AADAuthentication;
using ARMClient.Library;
using Newtonsoft.Json.Linq;
using SimpleWAWS;
using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Models
{
    public static class RbacHelper
    {
        private static readonly dynamic GraphClient;
        private static readonly dynamic CsmClient;
        private static readonly dynamic websitesCsmClient;
        static RbacHelper()
        {
            GraphClient = ARMLib.GetDynamicClient(apiVersion: ConfigurationManager.AppSettings["rbacGraphApiVersion"], url: string.Format("{0}/{1}", ConfigurationManager.AppSettings["graphApiBaseUrl"], ConfigurationManager.AppSettings["tryWebsitesTenantId"]))
                    .ConfigureLogin(LoginType.Upn, ConfigurationManager.AppSettings["grapAndCsmUserName"], ConfigurationManager.AppSettings["graphAndCsmPassword"]);

            CsmClient = ARMLib.GetDynamicClient(apiVersion: ConfigurationManager.AppSettings["rbacCsmApiVersion"])
                .ConfigureLogin(LoginType.Upn, ConfigurationManager.AppSettings["grapAndCsmUserName"], ConfigurationManager.AppSettings["graphAndCsmPassword"]);

            websitesCsmClient = ARMLib.GetDynamicClient(apiVersion: ConfigurationManager.AppSettings["websitesCsmClient"])
                .ConfigureLogin(LoginType.Upn, ConfigurationManager.AppSettings["grapAndCsmUserName"], ConfigurationManager.AppSettings["graphAndCsmPassword"]);

        }
        public static async Task<bool> AddRbacUser(string puidOrAltSec, string emailAddress, ResourceGroup resourceGroup)
        {
            if (string.IsNullOrEmpty(puidOrAltSec) ||
                puidOrAltSec.IndexOf("live.com", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return false;
            }

            var puid = puidOrAltSec.Split(':').Last();

            try
            {
                //check if user is already in directory
                var user = await GraphClient.Users.Query("$filter=netId eq '" + puid + "' or alternativeSecurityIds/any(x:x/type eq 1 and x/identityProvider eq null and x/key eq X'" + puid + "')").GetAsync<JObject>();
                string oid = null;
                if (user.value.Count == 0)
                {
                    // invite user
                    var invite = await GraphClient.Users.PostAsync<JObject>(new
                    {
                        creationType = "Invitation",
                        displayName = emailAddress,
                        primarySMTPAddress = emailAddress,
                        userType = "Guest"
                    });

                    // redeem invite
                    var redemption = await GraphClient.RedeemInvitation.PostAsync<JObject>(new
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
                // after adding a user, CSM can't find the user for a while.
                // pulling on Graph GET doesn't work, because that would return 200 while CSM still doesn't
                // recognize the new user.
                for (int i = 0; i < 30; i++)
                {
                    var response = (HttpResponseMessage)await CsmClient.Subscriptions[resourceGroup.SubscriptionId]
                                                            .ResourceGroups[resourceGroup.ResourceGroupName]
                                                            .Providers["Microsoft.Web"]
                                                            //.Sites[site.SiteName]
                                                            .Providers["Microsoft.Authorization"]
                                                            .RoleAssignments[resourceGroup.ResourceUniqueId]
                                                            .PutAsync(new
                                                            {
                                                                properties = new
                                                                {
                                                                    roleDefinitionId = "/subscriptions/" + resourceGroup.SubscriptionId + "/providers/Microsoft.Authorization/roleDefinitions/b24988ac-6180-42a0-ab88-20f7382dd24c",
                                                                    principalId = oid
                                                                }
                                                            });
                    if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        await Task.Delay(1000);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        await websitesCsmClient.Subscriptions[resourceGroup.SubscriptionId]
                                               .ResourceGroups[resourceGroup.ResourceGroupName]
                                               .Providers["Microsoft.Web"]
                                               //.Sites[site.SiteName]
                                               .PutAsync(new
                                               {
                                                   properties = new { },
                                                   location = "west us"
                                               });
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Fatal(e, AnalyticsEvents.ErrorInAddRbacUser, new { Puid = puidOrAltSec, Email = emailAddress });
            }
            return false;
        }

        public static async Task RemoveRbacUser(ResourceGroup resourceGroup)
        {
            try
            {
                //remove rbac policy
                var rbacClient = CsmClient.Subscriptions[resourceGroup.SubscriptionId]
                                          .ResourceGroups[resourceGroup.ResourceGroupName]
                                          .Providers["Microsoft.Web"]
                                          //.Sites[site.SiteName]
                                          .Providers["Microsoft.Authorization"]
                                          .RoleAssignments[resourceGroup.ResourceUniqueId];

                var rbacPolicy = await rbacClient.GetAsync<JObject>();
                await rbacClient.DeleteAsync();

                //remove user from tenant
                await GraphClient.Users[rbacPolicy.properties.principalId].DeleteAsync();
            }
            catch(Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, AnalyticsEvents.ErrorInRemoveRbacUser, resourceGroup.CsmId);
            }
        }

        public static async Task<bool> CheckIfRbacEnabled(ResourceGroup resourceGroup)
        {
            try
            {
                //remove rbac policy
                var rbacClient = CsmClient.Subscriptions[resourceGroup.SubscriptionId]
                                          .ResourceGroups[resourceGroup.ResourceGroupName]
                                          .Providers["Microsoft.Web"]
                                          //.Sites[site.SiteName]
                                          .Providers["Microsoft.Authorization"]
                                          .RoleAssignments[resourceGroup.ResourceUniqueId];

                var rbacPolicy = (HttpResponseMessage) await rbacClient.GetAsync();
                return rbacPolicy.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, AnalyticsEvents.ErrorInCheckRbacUser, resourceGroup.CsmId);
                return false;
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


    }
}