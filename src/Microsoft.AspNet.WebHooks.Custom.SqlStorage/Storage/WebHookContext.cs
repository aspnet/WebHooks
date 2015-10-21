using System.Data.Entity;

namespace Microsoft.AspNet.WebHooks.Storage
{
    public class WebHookContext : DbContext
    {

        public WebHookContext()
        {


        }


        public WebHookContext(string ConnectionString)
            : base(ConnectionString)
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("WebHooks");
        }

        public virtual DbSet<Registration> Registrations { get; set; }
    }
}
