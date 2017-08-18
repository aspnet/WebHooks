// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides context for an incoming WebHook request. The context is passed to registered <see cref="IWebHookHandler"/> implementations
    /// which can process the incoming request accordingly.
    /// </summary>
    public class WebHookHandlerContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookHandlerContext"/> with the given set of <paramref name="actions"/>.
        /// </summary>
        public WebHookHandlerContext(IEnumerable<string> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            Actions = actions.ToList();
        }

        /// <summary>
        /// Gets or sets a (potentially empty) ID of a particular configuration for this WebHook. This ID can be
        /// used to differentiate between WebHooks from multiple senders registered with the same receiver.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Provides the set of actions that caused the WebHook to be fired.
        /// </summary>
        public ICollection<string> Actions { get; }

        /// <summary>
        /// Gets or sets the optional data associated with this WebHook. The data typically represents the
        /// HTTP request entity body of the incoming WebHook but can have been processed in various ways
        /// by the corresponding <see cref="IWebHookReceiver"/>.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Gets the <see cref="HttpRequest"/> containing the WebHook.
        /// </summary>
        public HttpRequest Request => HttpContext?.Request;

        /// <summary>
        /// Gets or sets the <see cref="Http.HttpContext"/> for the incoming request.
        /// </summary>
        public HttpContext HttpContext { get; set; }

        /// <summary>
        /// Gets the <see cref="IActionResult"/> for the WebHook. If set by an <see cref="IWebHookHandler"/>, then
        /// execution of handlers will stop and the <see cref="Result"/> will be used in response to the WebHook
        /// request. If no handler sets this property, a default HTTP response will be sent.
        /// </summary>
        public IActionResult Result { get; set; }
    }
}