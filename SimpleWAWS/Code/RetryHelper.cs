using SimpleWAWS.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class RetryHelper
    {
        public static async Task Retry(Func<Task> func, int retryCount)
        {
            while(true)
            {
                try
                {
                    await func();
                    return;
                }
                catch (Exception e)
                {
                    if (retryCount <= 0) throw e;
                    retryCount--;
                }
            }
        }
    }
}