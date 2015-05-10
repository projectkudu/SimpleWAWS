using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Trace
{
    public static class SimpleTrace
    {
        public static ILogger Analytics;
        public static ILogger Diagnostics;
    }
}
