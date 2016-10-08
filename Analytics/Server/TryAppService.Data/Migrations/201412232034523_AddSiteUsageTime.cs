namespace TryAppService.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddSiteUsageTime : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SiteUsageTime",
                c => new
                    {
                        UniqueId = c.Guid(nullable: false),
                        SiteUsageTicks = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.UniqueId);
            
            AddColumn("dbo.UserActivity", "UniqueId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserActivity", "UniqueId");
            DropTable("dbo.SiteUsageTime");
        }
    }
}
