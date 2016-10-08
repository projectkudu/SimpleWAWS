using System;
using System.Threading;

namespace TryAppService.WebJob.Aggregation
{
    class Program
    {
        static void Main(string[] args)
        {
            var _lock = new object();
            var timer = new Timer((s) => {
                lock(_lock)
                {
                    var applicationLogsAnalyzer = new ApplicationLogAnalyzer();
                    applicationLogsAnalyzer.Analyze();

                    var iisLogsAnalyzer = new IISLogAnalyzer();
                    iisLogsAnalyzer.Analyze();
                }
            });
            timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(10));
            Console.ReadLine();
        }
    }
}
