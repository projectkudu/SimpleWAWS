using Microsoft.Azure.WebJobs;

namespace TryAppService.WebJob.ProcessQueue
{
    class Program
    {
        static void Main()
        {
            var host = new JobHost();
            host.RunAndBlock();
        }
    }
}
