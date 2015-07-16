using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TryAppService.Data.Models;

namespace TryAppService.Data.DataAccess
{
    public class TryItNowAnalyticsContext : DbContext
    {
        public TryItNowAnalyticsContext()
            : base("TryItNowAnalyticsContext")
        {
        }

        public DbSet<RequestsAggregate> RequestsAggregates { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<UserHourAggregate> UserHourAggregates { get; set; }
        public DbSet<TemplateUsageHourlyAggregate> TemplateUsageAggregates { get; set; }
        public DbSet<HttpStatusHourlyAggregate> HttpStatusAggregates { get; set; }
        public DbSet<UserPuid> UserPuids { get; set; }
        public DbSet<RefererAggregate> RefererAggregates { get; set; }
        public DbSet<SiteUsageTime> SiteUsageTimes { get; set; }
        public DbSet<UIEvent> UIEvents { get; set; }
        public DbSet<UserAssignedExperiment> UserAssignedExperiments { get; set; }
        public DbSet<UserLoggedIn> UserLoggedIns { get; set; }
        public DbSet<UserFeedback> UserFeedback { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }
    }
}
