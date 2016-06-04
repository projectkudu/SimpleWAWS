using Kudu.Client.Editor;
using Kudu.Client.Zip;
using Newtonsoft.Json;
using SimpleWAWS.Models;
using SimpleWAWS.Models.CsmModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        public static async Task<ServerFarm> Load(this ServerFarm serverFarm, CsmWrapper<CsmServerFarm> csmServerFarm = null)
        {
            Validate.ValidateCsmServerFarm(serverFarm);
            if (serverFarm.Sku["tier"] != Constants.TryAppServiceSku) return serverFarm;

            if (csmServerFarm == null)
            {
                var csmServerFarmResponse = await csmClient.HttpInvoke(HttpMethod.Get, ArmUriTemplates.ServerFarm.Bind(serverFarm));
                await csmServerFarmResponse.EnsureSuccessStatusCodeWithFullError();
                csmServerFarm = await csmServerFarmResponse.Content.ReadAsAsync<CsmWrapper<CsmServerFarm>>();
            }

            //serverFarm.Sku = csmServerFarm.sku;
            //serverFarm.Location = csmServerFarm.location;

            return serverFarm;
        }


    }
}