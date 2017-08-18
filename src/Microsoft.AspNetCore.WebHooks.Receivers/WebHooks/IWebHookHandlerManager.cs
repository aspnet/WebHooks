// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for managing <see cref="IWebHookHandler"/> instances which handle incoming WebHook requests.
    /// </summary>
    public interface IWebHookHandlerManager
    {
        /// <summary>
        /// Gets the list of available <see cref="IWebHookHandler"/> instances.
        /// </summary>
        IReadOnlyList<IWebHookHandler> Handlers { get; }
    }
}
