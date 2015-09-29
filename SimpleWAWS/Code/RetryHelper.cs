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
                await Task.Delay(1000);
            }
        }

        public static async Task<T> Retry<T>(Func<Task<T>> func, int retryCount)
        {
            while (true)
            {
                try
                {
                    return await func();
                }
                catch (Exception e)
                {
                    if (retryCount <= 0) throw e;
                    retryCount--;
                }
                await Task.Delay(1000);
            }
        }

    }
}