// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection
{
    // ??? Should we remove these do-nothing wrappers? I can see including them where the receiver requires specific
    // ??? configuration but am less comfortable with providing the wrappers by default.
    /// <summary>
    /// Extension methods for <see cref="HttpConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GitHubWebHookServiceCollectionExtensions
    {
        /// <summary>
        /// Initializes support for receiving GitHub WebHooks.
        /// Set the '<c>MS_WebHookReceiverSecret_GitHub</c>' application setting to the application secrets, optionally using IDs
        /// to differentiate between multiple WebHooks, for example '<c>secret0, id1=secret1, id2=secret2</c>'.
        /// The corresponding WebHook URI is of the form '<c>https://&lt;host&gt;/api/webhooks/incoming/github/{id}</c>'.
        /// For details about GitHub WebHooks, see <c>https://developer.github.com/webhooks/</c>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        public static void AddGitHubWebHooks(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddWebHooks();
        }

        /// <summary>
        /// Adds GitHub WebHooks services to the specified <paramref name="services"/> and calls
        /// <paramref name="setupAction"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">
        /// An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.
        /// </param>
        public static void AddGitHubWebHooks(this IServiceCollection services, Action<MvcOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddWebHooks(setupAction);
        }

        /// <summary>
        /// Adds GitHub WebHooks services to the specified <paramref name="services"/> and calls
        /// <paramref name="builderAction"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="builderAction">
        /// An <see cref="Action{IMvcCoreBuilder}"/> to configure the provided <see cref="IMvcCoreBuilder"/>.
        /// </param>
        public static void AddGitHubWebHooks(this IServiceCollection services, Action<IMvcCoreBuilder> builderAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (builderAction == null)
            {
                throw new ArgumentNullException(nameof(builderAction));
            }

            services.AddWebHooks(builderAction);
        }

        /// <summary>
        /// Adds GitHub WebHooks services to the specified <paramref name="services"/> and calls
        /// <paramref name="setupAction"/> then <paramref name="builderAction"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="setupAction">
        /// An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.
        /// </param>
        /// <param name="builderAction">
        /// An <see cref="Action{IMvcCoreBuilder}"/> to configure the provided <see cref="IMvcCoreBuilder"/>.
        /// </param>
        public static void AddGitHubWebHooks(
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

            services.AddWebHooks(setupAction, builderAction);
        }
    }
}