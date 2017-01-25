namespace TryAppService.Data.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HttpStatusHourlyAggregate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StatusCode = c.Int(nullable: false),
                        Count = c.Int(nullable: false),
                        Hour = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.RequestsAggregate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Path = c.String(),
                        Hits = c.Int(nullable: false),
                        Hour = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.TemplateUsageHourlyAggregate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Language = c.String(),
                        Total = c.Int(nullable: false),
                        Hour = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserActivity",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        DateTime = c.DateTime(nullable: false),
                        TemplateName = c.String(),
                        TemplateLanguage = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.UserHourAggregate",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        RequestsCount = c.Int(nullable: false),
                        Hour = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.UserHourAggregate");
            DropTable("dbo.UserActivity");
            DropTable("dbo.TemplateUsageHourlyAggregate");
            DropTable("dbo.RequestsAggregate");
            DropTable("dbo.HttpStatusHourlyAggregate");
        }
    }
}
