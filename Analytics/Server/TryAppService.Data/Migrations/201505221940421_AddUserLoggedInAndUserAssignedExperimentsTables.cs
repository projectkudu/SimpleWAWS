namespace TryAppService.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUserLoggedInAndUserAssignedExperimentsTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserAssignedExperiment",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        Experiment = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserLoggedIn",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AnonymousUserName = c.String(),
                        LoggedInUserName = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.UserLoggedIn");
            DropTable("dbo.UserAssignedExperiment");
        }
    }
}
