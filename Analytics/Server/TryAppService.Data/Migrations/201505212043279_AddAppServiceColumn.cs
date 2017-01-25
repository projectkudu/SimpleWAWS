namespace TryAppService.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddAppServiceColumn : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserActivity", "AppService", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserActivity", "AppService");
        }
    }
}
