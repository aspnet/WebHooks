// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.WebHooks.Routing;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Extension methods for the <see cref="RouteData"/> class.
    /// </summary>
    public static class WebHookRouteDataExtensions
    {
        /// <summary>
        /// Gets the event names for the current request.
        /// </summary>
        /// <param name="routeData">The <see cref="RouteData"/> for the current request.</param>
        /// <param name="eventNames">Set to the event names identified in the request.</param>
        /// <returns>
        /// <c>true</c> if event names were found in the <paramref name="routeData"/>; <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetEventNames(this RouteData routeData, out string[] eventNames)
        {
            if (routeData == null)
            {
                throw new ArgumentNullException(nameof(routeData));
            }

            if (routeData.Values.TryGetValue(WebHookReceiverRouteNames.EventKeyName, out var names))
            {
                eventNames = names as string[];
                return eventNames != null;
            }

            eventNames = null;
            return false;
        }

        /// <summary>
        /// Gets the receiver name for the current request.
        /// </summary>
        /// <param name="routeData">The <see cref="RouteData"/> for the current request.</param>
        /// <param name="receiverName">Set to the name of the requested receiver.</param>
        /// <returns>
        /// <c>true</c> if a receiver name was found in the <paramref name="routeData"/>; <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetReceiverName(this RouteData routeData, out string receiverName)
        {
            if (routeData == null)
            {
                throw new ArgumentNullException(nameof(routeData));
            }

            if (routeData.Values.TryGetValue(WebHookReceiverRouteNames.ReceiverKeyName, out var receiver))
            {
                receiverName = receiver as string;
                return receiverName != null;
            }

            receiverName = null;
            return false;
        }

        /// <summary>
        /// Gets the receiver id for the current request.
        /// </summary>
        /// <param name="routeData">The <see cref="RouteData"/> for the current request.</param>
        /// <param name="id">Set to the id of the requested receiver.</param>
        /// <returns>
        /// <c>true</c> if a receiver id was found in the <paramref name="routeData"/>; <c>false</c> otherwise.
        /// </returns>
        public static bool TryGetReceiverId(this RouteData routeData, out string id)
        {
            if (routeData == null)
            {
                throw new ArgumentNullException(nameof(routeData));
            }

            if (routeData.Values.TryGetValue(WebHookReceiverRouteNames.IdKeyName, out var identifier))
            {
                id = identifier as string;
                return id != null;
            }

            id = null;
            return false;
        }
    }
}
