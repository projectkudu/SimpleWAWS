namespace TryAppService.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddDateTimeColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserAssignedExperiment", "DateTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.UserLoggedIn", "DateTime", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserLoggedIn", "DateTime");
            DropColumn("dbo.UserAssignedExperiment", "DateTime");
        }
    }
}
