namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPropertiesToUiEvents : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UIEvent", "Properties", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UIEvent", "Properties");
        }
    }
}
