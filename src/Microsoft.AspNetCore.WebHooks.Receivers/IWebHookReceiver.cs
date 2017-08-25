// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for processing incoming WebHooks from a particular WebHook generator, for example
    /// <c>Dropbox</c>, <c>GitHub</c>, etc.
    /// </summary>
    public interface IWebHookReceiver
    {
        /// <summary>
        /// <para>
        /// Gets the case-insensitive name of the WebHook generator that this receiver supports, for example
        /// <c>dropbox</c> or <c>net</c>.
        /// </para>
        /// <para>
        /// The name provided here will map to a URI of the form
        /// '<c>https://&lt;host&gt;/api/webhooks/incoming/&lt;name&gt;</c>'.
        /// </para>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an indication that this <see cref="IWebHookReceiver"/> should execute in the current request.
        /// </summary>
        /// <param name="receiverName">The name of the <see cref="IWebHookReceiver"/> requested.</param>
        /// <returns>
        /// <c>true</c> if this <see cref="IWebHookReceiver"/> should execute; <c>false</c>otherwise.
        /// </returns>
        bool IsApplicable(string receiverName);
    }
}
