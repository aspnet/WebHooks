// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Microsoft.AspNetCore.WebHooks.Routing
{
    /// <summary>
    /// An <see cref="IActionConstraint"/> implementations which uses WebHook event names to select
    /// candidate actions.
    /// </summary>
    public class WebHookSingleEventSelectorConstraint : WebHookEventSelectorConstraint
    {
        private readonly string _eventName;
        private readonly string _pingEventName;

        /// <summary>
        /// Instantiates a new <see cref="WebHookEventSelectorConstraint"/> instance with the given
        /// <paramref name="eventName"/> and <paramref name="pingEventName"/>.
        /// </summary>
        /// <param name="eventName">Name of the event this action expects.</param>
        /// <param name="pingEventName">Name of the ping event for the receiver this action expects, if any.</param>
        public WebHookSingleEventSelectorConstraint(string eventName, string pingEventName)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            _eventName = eventName;

            // No need for extra handling if this constraint is for the ping event.
            if (!string.Equals(_eventName, _pingEventName, StringComparison.OrdinalIgnoreCase))
            {
                _pingEventName = pingEventName;
            }
        }

        /// <inheritdoc />
        public override bool Accept(ActionConstraintContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Accept(context, _eventName, _pingEventName);
        }
    }
}
