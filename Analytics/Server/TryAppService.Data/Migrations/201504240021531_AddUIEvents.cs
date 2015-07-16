namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUIEvents : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UIEvent",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        DateTime = c.DateTime(nullable: false),
                        EventName = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.UIEvent");
        }
    }
}
