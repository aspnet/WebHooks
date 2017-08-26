// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Manages registered <see cref="IWebHookHandler"/> instances.
    /// </summary>
    public class WebHookHandlerManager : IWebHookHandlerManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookHandlerManager"/> class with the given
        /// <paramref name="handlers"/>, <paramref name="sorter"/> and <paramref name="loggerFactory"/>.
        /// </summary>
        public WebHookHandlerManager(
            IEnumerable<IWebHookHandler> handlers,
            IWebHookHandlerSorter sorter,
            ILoggerFactory loggerFactory)
        {
            var sortedHandlers = sorter.SortHandlers(handlers);
            Handlers = new List<IWebHookHandler>(sortedHandlers);

            var logger = loggerFactory.CreateLogger<WebHookHandlerManager>();
            var receiverNames = string.Join(", ", Handlers.Select(h => h.Receiver));
            logger.LogInformation(
                0,
                "Registered '{Type}' instances for the following receivers: {ReceiverNames}.",
                nameof(IWebHookHandler),
                receiverNames);
        }

        /// <inheritdoc />
        public IReadOnlyList<IWebHookHandler> Handlers { get; }
    }
}