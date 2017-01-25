using System;
using System.Threading.Tasks;

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