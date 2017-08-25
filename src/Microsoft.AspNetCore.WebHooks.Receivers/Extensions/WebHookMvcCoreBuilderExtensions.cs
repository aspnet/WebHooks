// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebHooks;
using Microsoft.AspNetCore.WebHooks.Filters;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    // TODO: Add WebHookMvcBuilderExtensions variant of this class.
    /// <summary>
    /// Extension methods for setting up WebHooks in an <see cref="IMvcCoreBuilder" />.
    /// </summary>
    public static class WebHookMvcCoreBuilderExtensions
    {
        /// <summary>
        /// Adds WebHooks configuration and services to the specified <paramref name="services"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder" /> to configure.</param>
        public static IMvcCoreBuilder AddWebHooks(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var services = builder.Services;
            services.TryAddSingleton<IWebHookReceiverConfig, WebHookReceiverConfig>();

            builder.AddSingletonFilter<WebHookExceptionFilter>();

            return builder;
        }

        /// <summary>
        /// Add <typeparamref name="TFilter"/> as a singleton filter. Register <typeparamref name="TFilter"/> as a
        /// singleton service and add it to <see cref="AspNetCore.Mvc.MvcOptions.Filters"/>.
        /// </summary>
        /// <typeparam name="TFilter">The <see cref="IFilterMetadata"/> type to add.</typeparam>
        /// <param name="builder">The <see cref="IMvcCoreBuilder" /> to configure.</param>
        public static void AddSingletonFilter<TFilter>(this IMvcCoreBuilder builder)
            where TFilter : class, IFilterMetadata
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var services = builder.Services;
            services.TryAddSingleton<TFilter>();

            // Ensure the filter is available globally. Filter should no-op for non-WebHook requests.
            builder.AddMvcOptions(options =>
            {
                var filters = options.Filters;

                // TODO: Decide if any filters need non-default Order values.
                filters.AddService<TFilter>();
            });
        }
    }
}