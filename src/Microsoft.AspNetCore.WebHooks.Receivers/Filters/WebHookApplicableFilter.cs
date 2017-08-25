// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> that confirms at least one <see cref="IWebHookReceiver"/> is configured to
    /// handle a request.
    /// </summary>
    public class WebHookApplicableFilter : IResourceFilter
    {
        private readonly ILogger _logger;

        public WebHookApplicableFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WebHookApplicableFilter>();
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }

        /// <summary>
        /// Confirms at least one <see cref="IWebHookReceiver"/> is configured to handle the request. Sets
        /// <see cref="ResourceExecutingContext.Result"/> to a <see cref="NotFoundResult"/> when no applicable
        /// <see cref="IWebHookReceiver"/> is found in <see cref="FilterContext.Filters"/>.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var found = false;
            if (context.RouteData.TryGetReceiverName(out var receiverName))
            {
                for (var i = 0; i < context.Filters.Count; i++)
                {
                    var filter = context.Filters[i];
                    if (filter is IWebHookReceiver receiver && receiver.IsApplicable(receiverName))
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                _logger.LogError(
                    0,
                    "No WebHook receiver is registered with the name '{ReceiverName}'.",
                    receiverName);

                context.Result = new NotFoundResult();
                return;
            }

            context.RouteData.TryGetReceiverId(out var id);
            _logger.LogInformation(
                1,
                "Processing incoming WebHook request with receiver '{ReceiverName}' and id '{Id}'.",
                receiverName,
                id);
        }
    }
}
