using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using SimpleWAWS.Code;
using SimpleWAWS.Trace;

namespace SimpleWAWS.Models
{
    class Util
    {
        public static async Task SafeGuard(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "SafeGuard Exception");
            }
        }
        public static async Task<T> SafeGuard<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception e)
            {
                SimpleTrace.Diagnostics.Error(e, "SafeGuard<T> Exception");
                return default(T);
            }
        }

    }
}
