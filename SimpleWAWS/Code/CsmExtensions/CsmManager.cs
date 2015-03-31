using ARMClient.Library;
using SimpleWAWS.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code.CsmExtensions
{
    public static partial class CsmManager
    {
        private static readonly ARMLib csmClient;

        static CsmManager()
        {
            csmClient = ARMLib.GetDynamicClient(apiVersion: "")
                .ConfigureLogin(LoginType.Upn, Environment.GetEnvironmentVariable("TryUserName"), Environment.GetEnvironmentVariable("TryPassword"));
        }
    }
}
