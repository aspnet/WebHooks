// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    /// <summary>
    /// Metadata describing the security aspects of a WebHook request. Implemented in a <see cref="IWebHookMetadata"/>
    /// service for receivers that do not include a specific <see cref="Filters.WebHookSecurityFilter"/> subclass.
    /// </summary>
    public interface IWebHookSecurityMetadata : IWebHookMetadata, IWebHookReceiver
    {
        /// <summary>
        /// Gets an indication the <c>code</c> query parameter is required and should be compared with the configured
        /// secret key.
        /// </summary>
        bool VerifyCodeParameter { get; }
    }
}
