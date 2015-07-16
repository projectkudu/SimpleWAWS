namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRefererAndCampaignIdColumns : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserAssignedExperiment", "Referer", c => c.String());
            AddColumn("dbo.UserAssignedExperiment", "CampaignId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserAssignedExperiment", "CampaignId");
            DropColumn("dbo.UserAssignedExperiment", "Referer");
        }
    }
}
