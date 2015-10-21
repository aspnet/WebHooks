using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNet.WebHooks.Diagnostics;
using Microsoft.AspNet.WebHooks.Services;
using Microsoft.Framework.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace System.Web.Http
{
    public static class HttpConfigurationExtensions
    {
        private const string ApplicationName = "PE.WebHooks";
        private const string Purpose = "WebHookPersistence";
        private const string DataProtectionKeysFolderName = "DataProtection-Keys";

        /// <summary>
        /// Configures a Microsoft Sql Server Storage implementation of <see cref="IWebHookStore"/>
        /// which provides a persistent store for registered WebHooks used by the custom WebHooks module.
        /// </summary>
        /// <param name="config">The current <see cref="HttpConfiguration"/>config.</param>
        public static void InitializeCustomWebHooksSqlStorage(this HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            WebHooksConfig.Initialize(config);

            ILogger logger = config.DependencyResolver.GetLogger();
            SettingsDictionary settings = config.DependencyResolver.GetSettings();

            //Database.SetInitializer(new MigrateDatabaseToLatestVersion<WebHookContext, Configuration>());

            IDataProtectionProvider provider = GetDataProtectionProvider();
            IDataProtector protector = provider.CreateProtector(Purpose);

            IWebHookStore store = new SqlWebHookStore(settings, protector, logger);
            CustomServices.SetStore(store);
        }

        /// <summary>
        /// This follows the same initialization that is provided when <see cref="IDataProtectionProvider"/>
        /// is initialized within ASP.NET 5.0 Dependency Injection.
        /// </summary>
        /// <returns>A fully initialized <see cref="IDataProtectionProvider"/>.</returns>
        internal static IDataProtectionProvider GetDataProtectionProvider()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddDataProtection();
            IServiceProvider services = serviceCollection.BuildServiceProvider();
            return services.GetDataProtectionProvider();
        }
    }
}
