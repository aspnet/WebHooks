// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.WebHooks;
using Microsoft.AspNetCore.WebHooks.Receivers.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    // TODO: Add WebHookMvcBuilderExtensions variant of this class.
    // TODO: Remove requirement that all three public methods in this class are called.
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

            return builder;
        }

        // TODO: Add IApplicationFeatureProvider implementations that handle real default case and load handlers or
        // receivers from ApplicationParts. At the moment, calling these methods is required and applications that
        // add web parts elsewhere e.g. in other services are busted.
        public static void AddHandlersAsServices(this IMvcCoreBuilder builder)
        {
            var feature = new WebHookHandlerFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (var handler in feature.Handlers.Select(c => c.AsType()))
            {
                // ??? Am I correct handlers are inherently singletons unless explicitly added to DI?
                builder.Services.TryAddEnumerable(
                    ServiceDescriptor.Describe(typeof(IWebHookHandler), handler, ServiceLifetime.Singleton));
            }
        }

        public static void AddReceiversAsServices(this IMvcCoreBuilder builder)
        {
            var feature = new WebHookReceiverFeature();
            builder.PartManager.PopulateFeature(feature);

            foreach (var receiver in feature.Receivers.Select(c => c.AsType()))
            {
                // ??? Am I correct receivers are inherently singletons unless explicitly added to DI?
                builder.Services.TryAddEnumerable(
                    ServiceDescriptor.Describe(typeof(IWebHookReceiver), receiver, ServiceLifetime.Singleton));
            }
        }

        private static void AddEssentialServices(IMvcCoreBuilder builder)
        {
            ConfigureDefaultFeatureProviders(builder.PartManager);

            builder.AddJsonFormatters();
            builder.AddXmlSerializerFormatters();

            var services = builder.Services;
            services.TryAddSingleton<IWebHookHandlerManager, WebHookHandlerManager>();
            services.TryAddSingleton<IWebHookHandlerSorter, WebHookHandlerSorter>();
            services.TryAddSingleton<IWebHookReceiverConfig, WebHookReceiverConfig>();
            services.TryAddSingleton<IWebHookReceiverManager, WebHookReceiverManager>();
        }

        // TODO: Default should be case when neither AddHandlersAsServices() nor AddReceiversAsServices() is called.
        private static void ConfigureDefaultFeatureProviders(ApplicationPartManager manager)
        {
            if (!manager.FeatureProviders.OfType<WebHookHandlerFeatureProvider>().Any())
            {
                manager.FeatureProviders.Add(new WebHookHandlerFeatureProvider());
            }

            if (!manager.FeatureProviders.OfType<WebHookReceiverFeatureProvider>().Any())
            {
                manager.FeatureProviders.Add(new WebHookReceiverFeatureProvider());
            }
        }
    }
}
