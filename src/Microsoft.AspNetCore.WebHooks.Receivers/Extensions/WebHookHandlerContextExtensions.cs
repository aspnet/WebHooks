// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNet.WebHooks.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Various extension methods for the <see cref="WebHookHandlerContext"/> class.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WebHookHandlerContextExtensions
    {
        /// <summary>
        /// Gets the <see cref="WebHookHandlerContext.Data"/> property as type <typeparamref name="T"/>. If the
        /// contents is not of type <typeparamref name="T"/> then <c>null</c> is returned.
        /// </summary>
        /// <typeparam name="T">The type to convert <see cref="WebHookHandlerContext.Data"/> to.</typeparam>
        /// <param name="context">The <see cref="WebHookHandlerContext"/> to operate on.</param>
        /// <returns>An instance of type <typeparamref name="T"/> or <c>null</c> otherwise.</returns>
        public static T GetDataOrDefault<T>(this WebHookHandlerContext context)
            where T : class
        {
            if (context == null || context.Data == null)
            {
                return default(T);
            }

            // ??? Is IsAssignableFrom direction correct now? Looks backwards in Microsoft.AspNet.WebHooks.
            if (context.Data is JToken && !typeof(T).IsAssignableFrom(typeof(JToken)))
            {
                try
                {
                    var data = ((JToken)context.Data).ToObject<T>();
                    return data;
                }
                catch (Exception ex)
                {
                    // ??? Should this method's signature include the ILogger to avoid service locator pattern?
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger(typeof(WebHookHandlerContextExtensions));
                    logger.LogError(
                        0,
                        ex,
                        "Could not deserialize instance of type '{DataType}' as '{RequestedType}'.",
                        context.Data.GetType(),
                        typeof(T));

                    return default(T);
                }
            }

            // ??? Isn't !(context.Data is T) worth logging or even throwing about?
            return context.Data as T;
        }

        /// <summary>
        /// Tries getting the <see cref="WebHookHandlerContext.Data"/> property as type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to convert <see cref="WebHookHandlerContext.Data"/> to.</typeparam>
        /// <param name="context">The <see cref="WebHookHandlerContext"/> to operate on.</param>
        /// <param name="value">The converted value.</param>
        /// <returns>An instance of type <typeparamref name="T"/> or <c>null</c> otherwise.</returns>
        public static bool TryGetData<T>(this WebHookHandlerContext context, out T value)
            where T : class
        {
            value = GetDataOrDefault<T>(context);
            return value != default(T);
        }

        public static void CreateErrorResult(this WebHookHandlerContext context, int statusCode, string message)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var error = new SerializableError
            {
                { SerializableErrorKeys.MessageKey, message },
            };
            context.Result = new BadRequestObjectResult(error)
            {
                StatusCode = statusCode,
            };
        }
    }
}
