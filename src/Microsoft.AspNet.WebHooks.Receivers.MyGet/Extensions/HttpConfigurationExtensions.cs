﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel;
using Microsoft.AspNet.WebHooks.Config;

namespace System.Web.Http
{
    /// <summary>
    /// Extension methods for <see cref="HttpConfiguration"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        /// <summary>
        /// Initializes support for receiving MyGet WebHooks.
        /// For details about MyGet WebHooks, see <c>http://docs.myget.org/docs/reference/webhooks</c>.
        /// </summary>
        /// <param name="config">The current <see cref="HttpConfiguration"/>config.</param>
        public static void InitializeReceiveMyGetWebHooks(this HttpConfiguration config)
        {
            WebHooksConfig.Initialize(config);
        }
    }
}
