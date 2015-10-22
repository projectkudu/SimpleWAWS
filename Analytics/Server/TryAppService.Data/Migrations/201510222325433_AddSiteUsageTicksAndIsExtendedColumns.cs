namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSiteUsageTicksAndIsExtendedColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserActivity", "IsExtended", c => c.Boolean(nullable: false));
            AddColumn("dbo.UserActivity", "SiteUsageTicks", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserActivity", "SiteUsageTicks");
            DropColumn("dbo.UserActivity", "IsExtended");
        }
    }
}
