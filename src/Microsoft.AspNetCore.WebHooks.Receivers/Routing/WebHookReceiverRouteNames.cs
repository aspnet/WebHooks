// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks.Routing
{
    /// <summary>
    /// Provides a set of common route names used for receiving incoming WebHooks.
    /// </summary>
    public static class WebHookReceiverRouteNames
    {
        /// <summary>
        /// Gets the name of the <see cref="AspNetCore.Routing.RouteValueDictionary"/> entry containing the event name
        /// for the current request.
        /// </summary>
        public static string EventKeyName => "event";

        /// <summary>
        /// Gets the name of the <see cref="AspNetCore.Routing.RouteValueDictionary"/> entry containing the receiver id
        /// for the current request.
        /// </summary>
        public static string IdKeyName => "id";

        /// <summary>
        /// Gets the name of the <see cref="AspNetCore.Routing.RouteValueDictionary"/> entry containing the receiver
        /// name for the current request.
        /// </summary>
        public static string ReceiverKeyName => "webHookReceiver";

        /// <summary>
        /// Gets the name of the <see cref="AspNetCore.Routing.RouteValueDictionary"/> entry containing an indication
        /// the <see cref="WebHookReceiverExistsConstraint"/> successfully confirmed the request matched a configured
        /// receiver.
        /// </summary>
        public static string ReceiverExistsKeyName => nameof(WebHookReceiverExistsConstraint);

        // ??? This is not currently referenced. Was used as route name for the single action in the old world.
        /// <summary>
        /// Gets the name of the route for receiving generic WebHook requests.
        /// </summary>
        public static string ReceiverRouteName => "ReceiversAction";
    }
}
