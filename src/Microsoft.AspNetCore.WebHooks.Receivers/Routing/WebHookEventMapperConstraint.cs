// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Routing
{
    /// <summary>
    /// An base class for <see cref="IActionConstraint"/> implementations which use
    /// <see cref="IWebHookEventMetadata"/> to determine the event name for a WebHook request. This constraint
    /// almost-always accepts all candidates.
    /// </summary>
    public abstract class WebHookEventMapperConstraint : IActionConstraint
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="WebHookEventMapperConstraint"/> instance with the given
        /// <paramref name="loggerFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        protected WebHookEventMapperConstraint(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger(GetType());
        }

        /// <summary>
        /// Gets the <see cref="IActionConstraint.Order"/> value used in all <see cref="WebHookEventMapperConstraint"/>
        /// instances.
        /// </summary>
        /// <value>Chosen to run this constraint early in action selection.</value>
        public static int Order => -500;

        /// <inheritdoc />
        int IActionConstraint.Order => Order;

        /// <inheritdoc />
        public abstract bool Accept(ActionConstraintContext context);

        // ??? Any need to either special-case events.Length==1 or (if not) create a comma-separated string? For now,
        // ??? consistently adds a string[] to the route values.
        /// <summary>
        /// Gets an indication whether the expected event names are available in the request.
        /// </summary>
        /// <param name="constantValue">
        /// An array containing the fallback constant value for this receiver. <c>null</c> if the
        /// <paramref name="eventMetadata"/>'s <see cref="IWebHookEventMetadata.ConstantValue"/> is <c>null</c> or
        /// empty.
        /// </param>
        /// <param name="eventMetadata">The <see cref="IWebHookEventMetadata"/> for this receiver.</param>
        /// <param name="routeContext">The <see cref="RouteContext"/> for this constraint.</param>
        /// <returns><c>true</c> if event names are available in the request; <c>false</c> otherwise.</returns>
        protected bool Accept(string[] constantValue, IWebHookEventMetadata eventMetadata, RouteContext routeContext)
        {
            if (eventMetadata == null)
            {
                throw new ArgumentNullException(nameof(eventMetadata));
            }

            if (routeContext == null)
            {
                throw new ArgumentNullException(nameof(routeContext));
            }

            var request = routeContext.HttpContext.Request;
            var routeData = routeContext.RouteData;
            var routeValues = routeData.Values;
            if (eventMetadata.HeaderName != null)
            {
                var headers = request.Headers;

                // ??? Is GetCommaSeparatedValues() overkill?
                var events = headers.GetCommaSeparatedValues(eventMetadata.HeaderName);
                if (events.Length == 0)
                {
                    if (constantValue == null)
                    {
                        // An error because we have no fallback. HeaderName and QueryParameterKey aren't used together.
                        routeData.TryGetReceiverName(out var receiverName);
                        _logger.LogError(
                            500,
                            "A {ReceiverName} WebHook request must contain a '{HeaderName}' HTTP header indicating the type of event.",
                            receiverName,
                            eventMetadata.HeaderName);
                    }
                }
                else
                {
                    routeValues[WebHookReceiverRouteNames.EventKeyName] = events;
                    return true;
                }
            }

            if (eventMetadata.QueryParameterKey != null)
            {
                var query = request.Query;
                if (!query.TryGetValue(eventMetadata.QueryParameterKey, out var events) ||
                    events.Count == 0)
                {
                    if (constantValue == null)
                    {
                        // An error because we have no fallback. HeaderName and QueryParameterKey aren't used together.
                        routeData.TryGetReceiverName(out var receiverName);
                        _logger.LogError(
                            501,
                            "A {ReceiverName} WebHook request must contain a '{QueryParameterKey}' query parameter indicating the type of event.",
                            receiverName,
                            eventMetadata.QueryParameterKey);
                    }
                }
                else
                {
                    routeValues[WebHookReceiverRouteNames.EventKeyName] = (string[])events;
                    return true;
                }
            }

            if (constantValue != null)
            {
                routeValues[WebHookReceiverRouteNames.EventKeyName] = constantValue;
                return true;
            }

            return false;
        }
    }
}
