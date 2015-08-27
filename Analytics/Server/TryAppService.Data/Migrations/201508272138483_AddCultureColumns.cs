namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCultureColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UIEvent", "UserCulture", c => c.String());
            AddColumn("dbo.UserActivity", "UserCulture", c => c.String());
            AddColumn("dbo.UserAssignedExperiment", "UserCulture", c => c.String());
            AddColumn("dbo.UserFeedback", "UserCulture", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserFeedback", "UserCulture");
            DropColumn("dbo.UserAssignedExperiment", "UserCulture");
            DropColumn("dbo.UserActivity", "UserCulture");
            DropColumn("dbo.UIEvent", "UserCulture");
        }
    }
}
