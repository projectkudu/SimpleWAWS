namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSourceVariationAndAnonymousUserNameColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UIEvent", "SourceVariation", c => c.String());
            AddColumn("dbo.UIEvent", "AnonymousUserName", c => c.String());
            AddColumn("dbo.UserActivity", "SourceVariation", c => c.String());
            AddColumn("dbo.UserActivity", "AnonymousUserName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserActivity", "AnonymousUserName");
            DropColumn("dbo.UserActivity", "SourceVariation");
            DropColumn("dbo.UIEvent", "AnonymousUserName");
            DropColumn("dbo.UIEvent", "SourceVariation");
        }
    }
}
