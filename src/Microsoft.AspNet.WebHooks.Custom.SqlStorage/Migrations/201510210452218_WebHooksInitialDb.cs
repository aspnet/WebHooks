namespace Microsoft.AspNet.WebHooks.Custom.SqlStorage.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class WebHooksInitialDb : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "WebHooks.WebHooks",
                c => new
                    {
                        User = c.String(nullable: false, maxLength: 256),
                        Id = c.String(nullable: false, maxLength: 256),
                        ProtectedData = c.String(nullable: false),
                        RowVer = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => new { t.User, t.Id });
            
        }
        
        public override void Down()
        {
            DropTable("WebHooks.WebHooks");
        }
    }
}
