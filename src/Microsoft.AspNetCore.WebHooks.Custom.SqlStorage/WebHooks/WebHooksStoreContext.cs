using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Defines a <see cref="DbContext"/> which contains the set of WebHook <see cref="Registration"/> instances.
    /// </summary>
    public class WebHooksStoreContext : DbContext
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHooksStoreContext"/> class.
        /// </summary>
        public WebHooksStoreContext() : base()
        {
        }

        /// <summary>
        /// Gets or sets the current collection of <see cref="Registration"/> instances.
        /// </summary>
        public virtual DbSet<Registration> Registrations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException("modelBuilder");
            }

            modelBuilder.HasDefaultSchema("WebHooks");
        }
    }
}
