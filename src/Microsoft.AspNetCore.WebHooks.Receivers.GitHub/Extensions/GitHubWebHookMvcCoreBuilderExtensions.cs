// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.WebHooks.Filters;

namespace Microsoft.Extensions.DependencyInjection
{
    // TODO: Add WebHookMvcBuilderExtensions variant of this class.
    /// <summary>
    /// Extension methods for setting up GitHub WebHooks in an <see cref="IMvcCoreBuilder" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class GitHubWebHookMvcCoreBuilderExtensions
    {
        // The ConsumesAttribute and our WebHookActionAttribute subclasses do not implement IOrderedFilter.
        private const int ConsumesAttributeFilterOrder = 0;

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
            builder.AddJsonFormatters();
            builder.AddWebHooks();

            // 1. Confirm it's a POST request
            // 2. Confirm signature
            // 3. Confirm Body contains JSON i.e. execute ConsumesAttribute.OnResourceExecuting()
            // 4. Short-circuit ping requests
            builder.AddSingletonFilter<GitHubWebHookVerifyActionFilter>(ConsumesAttributeFilterOrder + 10);
            builder.AddSingletonFilter<GitHubWebHookVerifyMethodFilter>(ConsumesAttributeFilterOrder - 20);
            builder.AddSingletonFilter<GitHubWebHookVerifySignatureFilter>(ConsumesAttributeFilterOrder - 10);

            return builder;
        }
    }
}