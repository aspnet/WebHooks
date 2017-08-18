// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.WebHooks;
using Microsoft.AspNetCore.WebHooks.Receivers.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up WebHooks services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class WebHookServiceCollectionExtensions
    {
        // ??? Do we need variants of these methods which call AddMvc() and optionally accept an Action<IMvcBuilder>?
        /// <summary>
        /// Adds WebHooks services to the specified <paramref name="services"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>An <see cref="IMvcCoreBuilder"/> that can be used to further configure underlying MVC services.</returns>
        public static void AddWebHooks(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var builder = AddEssentialServices(services);

            AddHandlersAsServices(builder);
            AddReceiversAsServices(builder);
        }

        /// <summary>
        /// Adds WebHooks services to the specified <paramref name="services"/> and calls <paramref name="setupAction"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">
        /// An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.
        /// </param>
        public static void AddWebHooks(this IServiceCollection services, Action<MvcOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            var builder = AddEssentialServices(services);
            builder.AddMvcOptions(setupAction);

            AddHandlersAsServices(builder);
            AddReceiversAsServices(builder);
        }

        /// <summary>
        /// Adds WebHooks services to the specified <paramref name="services"/> and calls <paramref name="builderAction"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="builderAction">
        /// An <see cref="Action{IMvcCoreBuilder}"/> to configure the provided <see cref="IMvcCoreBuilder"/>.
        /// </param>
        public static void AddWebHooks(this IServiceCollection services, Action<IMvcCoreBuilder> builderAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (builderAction == null)
            {
                throw new ArgumentNullException(nameof(builderAction));
            }

            var builder = AddEssentialServices(services);
            builderAction(builder);

            AddHandlersAsServices(builder);
            AddReceiversAsServices(builder);
        }

        // ??? Is this needed when users can call builder.AddMvcOptions(setupAction) in their builderAction?
        /// <summary>
        /// Adds WebHooks services to the specified <paramref name="services"/> and calls <paramref name="setupAction"/>
        /// then <paramref name="builderAction"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">
        /// An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.
        /// </param>
        /// <param name="builderAction">
        /// An <see cref="Action{IMvcCoreBuilder}"/> to configure the provided <see cref="IMvcCoreBuilder"/>.
        /// </param>
        public static void AddWebHooks(
            this IServiceCollection services,
            Action<MvcOptions> setupAction,
            Action<IMvcCoreBuilder> builderAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }
            if (builderAction == null)
            {
                throw new ArgumentNullException(nameof(builderAction));
            }

            var builder = AddEssentialServices(services);
            builder.AddMvcOptions(setupAction);
            builderAction(builder);

            AddHandlersAsServices(builder);
            AddReceiversAsServices(builder);
        }

        private static IMvcCoreBuilder AddEssentialServices(IServiceCollection services)
        {
            var builder = services.AddMvcCore();
            ConfigureDefaultFeatureProviders(builder.PartManager);

            services.TryAddSingleton<IWebHookHandlerManager, WebHookHandlerManager>();
            services.TryAddSingleton<IWebHookHandlerSorter, WebHookHandlerSorter>();
            services.TryAddSingleton<IWebHookReceiverConfig, WebHookReceiverConfig>();
            services.TryAddSingleton<IWebHookReceiverManager, WebHookReceiverManager>();

            return builder;
        }

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

        // ??? Everyone agree above methods should return the IMvcCoreBuilder (or a derived interface) and these two
        // ??? should be extensions on that interface? Current approach avoids problems with later AddApplicationPart()
        // ??? calls but not ApplicationPart changes made in services. Would be more flexible if we default to
        // ??? IWebHookHandlerManager / IWebHookReceiverManager implementations that activate the instances but allow
        // ??? overrides to current classes. (May need to check DI then fall back to using ApplicationPartManager in
        // ??? default case. That allows users to make explicit service lifetime choices.)
        private static void AddHandlersAsServices(IMvcCoreBuilder builder)
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

        private static void AddReceiversAsServices(IMvcCoreBuilder builder)
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
    }
}
