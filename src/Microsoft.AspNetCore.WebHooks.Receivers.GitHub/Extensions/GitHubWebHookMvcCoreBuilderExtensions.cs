// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.WebHooks.Filters;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    // TODO: Add WebHookMvcBuilderExtensions variant of this class.
    /// <summary>
    /// Extension methods for setting up GitHub WebHooks in an <see cref="IMvcCoreBuilder" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GitHubWebHookMvcCoreBuilderExtensions
    {
        /// <summary>
        /// Add GitHub WebHooks configuration and services to the specified <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcCoreBuilder" /> to configure.</param>
        public static IMvcCoreBuilder AddGitHubWebHooks(this IMvcCoreBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWebHookMetadata, GitHubWebHookMetadata>());

            return builder
                .AddJsonFormatters()
                .AddWebHooks()
                .AddSingletonFilter<GitHubWebHookVerifySignatureFilter>(WebHookReceiverFilter.Order);
        }
    }
}