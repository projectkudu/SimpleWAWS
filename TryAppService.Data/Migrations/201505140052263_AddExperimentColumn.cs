namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddExperimentColumn : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UIEvent", "Experiment", c => c.String());
            AddColumn("dbo.UserActivity", "Experiment", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserActivity", "Experiment");
            DropColumn("dbo.UIEvent", "Experiment");
        }
    }
}
