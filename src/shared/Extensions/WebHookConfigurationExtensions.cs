// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="IConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class WebHookConfigurationExtensions
    {
        /// <summary>
        /// Returns <see langword="true"/> if the configuration value with given <paramref name="key"/> is set to
        /// 'true'; otherwise <see langword="false"/>.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        /// <param name="key">The key of the configuration value to evaluate.</param>
        /// <returns><see langword="true"/> if the value is set to 'true'; otherwise <see langword="false"/>.</returns>
        public static bool IsTrue(this IConfiguration configuration, string key)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var value = configuration[key];
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return bool.TryParse(value.Trim(), out var isSet) ? isSet : false;
        }
    }
}
