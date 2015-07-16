using System.Data.Entity.Migrations;
using TryAppService.Data.DataAccess;

namespace TryAppService.Data.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<TryItNowAnalyticsContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(TryItNowAnalyticsContext context)
        {
        }
    }
}
