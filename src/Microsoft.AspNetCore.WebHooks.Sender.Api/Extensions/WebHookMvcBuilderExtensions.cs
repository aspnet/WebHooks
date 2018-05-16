using System;
using System.Reflection;
using Microsoft.AspNetCore.WebHooks.Controllers;
using Microsoft.AspNetCore.WebHooks.WebHooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.WebHooks.Extensions
{
    /// <summary>
    /// Extension methods for setting up WebHooks in an <see cref="IMvcBuilder" />.
    /// </summary>
    public static class WebHookMvcBuilderExtensions
    {
        /// <summary>
        /// <para>
        /// Add WebHook configuration and services to the specified <paramref name="builder"/>.
        /// </para>        
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder" /> to configure.</param>
        /// <returns>The <paramref name="builder"/>.</returns>
        public static IMvcBuilder AddWebHooks(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddApplicationPart(typeof(WebHookRegistrationsController).GetTypeInfo().Assembly).AddControllersAsServices();

            builder.Services.TryAddSingleton<IWebHookSender, DataflowWebHookSender>();
            builder.Services.TryAddSingleton<IWebHookStore, MemoryWebHookStore>();
            builder.Services.TryAddSingleton<IWebHookManager, WebHookManager>();
            builder.Services.TryAddTransient<IWebHookFilterManager, WebHookFilterManager>();
            builder.Services.TryAddTransient<IWebHookRegistrationsManager, WebHookRegistrationsManager>();
            builder.Services.TryAddTransient<IWebHookUser, WebHookUser>();
            builder.Services.TryAddTransient<IWebHookIdValidator, DefaultWebHookIdValidator>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IWebHookFilterProvider, WildcardWebHookFilterProvider>());

            return builder;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddWebHookFilterProvider<T>(this IMvcBuilder builder) where T : class, IWebHookFilterProvider
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IWebHookFilterProvider, T>());
            return builder;
        }
    }
}
