// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for managing <see cref="IWebHookReceiver"/> configuration. This makes it possible
    /// to manage configuration of secrets in a consistent manner separately of any given <see cref="IWebHookReceiver"/>.
    /// </summary>
    public interface IWebHookReceiverConfig
    {
        /// <summary>
        /// Gets the application's <see cref="IConfiguration"/>.
        /// </summary>
        /// <remarks>
        /// Primarily for convenience; avoids consumers having to get the <see cref="IConfiguration"/> separately.
        /// </remarks>
        IConfiguration Configuration { get; }

        /// <summary>
        /// Gets the receiver configuration for a given <paramref name="name"/> and a particular <paramref name="id"/>
        /// or <c>null</c> if not found.
        /// </summary>
        /// <param name="name">The case-insensitive name of the receiver configuration used by the incoming WebHook. The receiver
        /// name can for example be <c>dropbox</c> or <c>github</c>.</param>
        /// <param name="id">A (possibly empty) ID of a particular configuration for the given <paramref name="name"/>.
        /// This can be used for one receiver to differentiate between multiple configurations.</param>
        /// <returns>The requested config, or <c>null</c> if not found.</returns>
        Task<string> GetReceiverConfigAsync(string name, string id);
    }
}
