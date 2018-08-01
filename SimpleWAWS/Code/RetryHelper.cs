using System;
using System.Threading.Tasks;

namespace SimpleWAWS.Code
{
    public static class RetryHelper
    {
        public static int _delay = 1000;
        public static async Task Retry(Func<Task> func, int retryCount, int delay = 1000)
        {
            _delay = delay;
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
                await Task.Delay(_delay);
            }
        }

        public static async Task<T> Retry<T>(Func<Task<T>> func, int retryCount, int delay = 1000)
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
                await Task.Delay(_delay);
            }
        }

    }
}