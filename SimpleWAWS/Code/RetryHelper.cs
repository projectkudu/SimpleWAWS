using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Code
{
    public static class RetryHelper
    {
        public static T Retry<T>(Func<T> func, int retryCount)
        {
            return func();
        }

    }
}