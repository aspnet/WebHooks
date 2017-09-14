// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> to short-circuit ping WebHook requests.
    /// </summary>
    public class WebHookPingResponseFilter : IResourceFilter, IOrderedFilter
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyList<IWebHookEventMetadata> _eventMetadata;

        /// <summary>
        /// Instantiates a new <see cref="WebHookPingResponseFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="metadata">The collection of <see cref="IWebHookMetadata"/> services.</param>
        public WebHookPingResponseFilter(ILoggerFactory loggerFactory, IEnumerable<IWebHookMetadata> metadata)
        {
            _logger = loggerFactory.CreateLogger<WebHookVerifyBodyTypeFilter>();
            _eventMetadata = new List<IWebHookEventMetadata>(metadata.OfType<IWebHookEventMetadata>());
        }

        /// <summary>
        /// Gets the <see cref="IOrderedFilter.Order"/> used in all <see cref="WebHookVerifyBodyTypeFilter"/>
        /// instances. The recommended filter sequence is
        /// <list type="number">
        /// <item><description>
        /// Confirm signature e.g. in a <see cref="WebHookReceiverFilter"/> subclass.
        /// </description></item>
        /// <item><description>Short-circuit GET or HEAD requests, if receiver supports either.</description></item>
        /// <item>
        /// <description>Confirm it's a POST request (<see cref="WebHookVerifyMethodFilter"/>).</description>
        /// </item>
        /// <item><description>Confirm body type (<see cref="WebHookVerifyBodyTypeFilter"/>).</description></item>
        /// <item><description>
        /// Short-circuit ping requests, if not done in #2 for this receiver (this filter).
        /// </description></item>
        /// </list>
        /// </summary>
        public static int Order => WebHookVerifyBodyTypeFilter.Order + 10;

        /// <inheritdoc />
        int IOrderedFilter.Order => Order;

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var routeData = context.RouteData;
            if (routeData.TryGetEventNames(out var eventNames) &&
                routeData.TryGetReceiverName(out var receiverName))
            {
                var eventMetadata = _eventMetadata.FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                var pingEventName = eventMetadata?.PingEventName;

                // If this is a ping request, short-circuit further processing.
                if (pingEventName != null &&
                    eventNames.Any(name => string.Equals(name, pingEventName, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation(0, "Received a {ReceiverName} Ping Event -- ignoring.", receiverName);

                    context.Result = new OkResult();
                    return;
                }
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }
    }
}
