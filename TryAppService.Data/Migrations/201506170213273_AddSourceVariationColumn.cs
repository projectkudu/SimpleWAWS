namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSourceVariationColumn : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserAssignedExperiment", "SourceVariation", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserAssignedExperiment", "SourceVariation");
        }
    }
}
