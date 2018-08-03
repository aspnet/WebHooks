// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up WebHooks in an <see cref="IMvcBuilder" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WebHookMvcBuilderExtensions
    {
        /// <summary>
        /// <para>
        /// Add WebHook configuration and services to the specified <paramref name="builder"/>.
        /// </para>
        /// <para>
        /// '<c>WebHooks:{receiver name}:SecretKey:default</c>' configuration values usually contain secret keys for
        /// WebHook URIs of the form '<c>https://{host}/api/webhooks/incoming/{receiver name}</c>' (with a
        /// <c>?code=...</c> query string for some receivers). '<c>WebHooks:{receiver name}:SecretKey:{id}</c>'
        /// configuration values usually contain secret keys for WebHook URIs of the form
        /// '<c>https://{host}/api/webhooks/incoming/{receiver name}/{id}</c>'.
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

            WebHookServiceCollectionSetup.AddWebHookServices(builder.Services);

            return builder;
        }
    }
}
