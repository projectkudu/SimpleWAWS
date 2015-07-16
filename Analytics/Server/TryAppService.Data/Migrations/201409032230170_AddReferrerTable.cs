namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddReferrerTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RefererAggregate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Path = c.String(),
                        Count = c.Int(nullable: false),
                        Hour = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.RefererAggregate");
        }
    }
}
