// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.WebHooks
{
    // TODO: Should probably flesh this out to match HttpRequestMessage extension methods and ease transitions.
    // TODO: May also need CreateResponse() equivalents.
    // ??? Would a bunch of HttpRequest extension methods that add an IActionResult to a well-known entry in
    // ??? request.HttpContext.Items work better? Seems very WebApiCompatShim-like.
    /// <summary>
    /// Methods shared between <see cref="WebHookHandler"/> and <see cref="WebHookReceiver"/>. All return
    /// <see cref="IActionResult"/>s and are similar to Web API HttpResultMessage methods or extensions.
    /// </summary>
    public abstract class WebHookSharedResultBase
    {
        public IActionResult CreateErrorResult(int statusCode, string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var error = new SerializableError
            {
                { WebHookErrorKeys.MessageKey, message },
            };
            var result = new BadRequestObjectResult(error)
            {
                StatusCode = statusCode,
            };

            return result;
        }
    }
}
