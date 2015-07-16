namespace TryAppService.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserFeedbackTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserFeedback",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserName = c.String(),
                        AnonymousUserName = c.String(),
                        Comment = c.String(),
                        ContactMe = c.Boolean(nullable: false),
                        DateTime = c.DateTime(nullable: false),
                        Experiment = c.String(),
                        SourceVariation = c.String(),
                    })
                .PrimaryKey(t => t.Id);
        }

        public override void Down()
        {
            DropTable("dbo.UserFeedback");
        }
    }
}