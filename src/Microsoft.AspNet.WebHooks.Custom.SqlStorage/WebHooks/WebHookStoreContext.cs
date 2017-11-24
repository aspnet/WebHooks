// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using Microsoft.AspNet.WebHooks.Storage;

namespace Microsoft.AspNet.WebHooks
{
    /// <summary>
    /// Defines a <see cref="DbContext"/> which contains the set of WebHook <see cref="Registration"/> instances.
    /// </summary>
    public class WebHookStoreContext : DbContext
    {
        private static string _connectionStringName = "MS_SqlStoreConnectionString";
        private static string _schemaName = "WebHooks";
        private static string _tableName = "WebHooks";

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookStoreContext"/> class.
        /// </summary>
        public WebHookStoreContext() : base(ConnectionStringName)
        {
        }

        /// <summary>
        /// Gets or sets the name of connection string. Default value is MS_SqlStoreConnectionString
        /// </summary>
        public static string ConnectionStringName
        {
            get
            {
                return _connectionStringName;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _connectionStringName = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of schema. Default value is WebHooks
        /// </summary>
        public static string SchemaName
        {
            get
            {
                return _schemaName;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _schemaName = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of table. Default value is WebHooks
        /// </summary>
        public static string TableName
        {
            get
            {
                return _tableName;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _tableName = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current collection of <see cref="Registration"/> instances.
        /// </summary>
        public virtual DbSet<Registration> Registrations { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            modelBuilder.HasDefaultSchema(SchemaName);
            EntityTypeConfiguration<Registration> registrationConfiguration = modelBuilder.Entity<Registration>();
            registrationConfiguration.ToTable(TableName);
        }
    }
}
