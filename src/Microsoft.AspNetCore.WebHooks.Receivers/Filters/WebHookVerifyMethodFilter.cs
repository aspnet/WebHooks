// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> to constrain the accepted HTTP methods (POST and optionally GET) on WebHook
    /// requests. Most receiver projects should register a subclass. If <see cref="AllowGet"/> is <c>true</c>, the
    /// receiver project should register another <see cref="IResourceFilter"/> which always short-circuits GET requests.
    /// </summary>
    /// <remarks>
    /// The separate <see cref="IResourceFilter"/> implementation avoids multiple actions in user projects. GET
    /// requests (often pings or simple verifications) are never of interest in user code.
    /// </remarks>
    public abstract class WebHookVerifyMethodFilter : WebHookReceiver, IResourceFilter
    {
        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifyMethodFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="Logger"/>.
        /// </param>
        /// <param name="receiverConfig">The <see cref="IWebHookReceiverConfig"/>.</param>
        protected WebHookVerifyMethodFilter(ILoggerFactory loggerFactory, IWebHookReceiverConfig receiverConfig)
            : base(loggerFactory, receiverConfig)
        {
        }

        /// <summary>
        /// Gets an indication whether the GET method should be allowed for applicable Webhook requests.
        /// </summary>
        /// <value>If <c>true</c>, GET requests are allowed; otherwise GET requests result in a 405 response.</value>
        public abstract bool AllowGet { get; }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.RouteData.TryGetReceiverName(out var receiver) && IsApplicable(receiver))
            {
                var request = context.HttpContext.Request;
                var method = request.Method;
                if (!(HttpMethods.IsPost(method) || (AllowGet && HttpMethods.IsGet(method))))
                {
                    // Log about the issue and short-circuit remainder of the pipeline.
                    context.Result = CreateBadMethodResult(request);
                }
            }
        }

        private IActionResult CreateBadMethodResult(HttpRequest request)
        {
            Logger.LogError(
                400,
                "The HTTP '{RequestMethod}' method is not supported by the '{ReceiverType}' WebHook receiver.",
                request.Method,
                GetType().Name);

            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Resources.Receiver_BadMethod,
                request.Method,
                GetType().Name);

            // ??? Should we instead provide CreateErrorResult(...) overloads with `int statusCode` parameters?
            var badMethod = WebHookResultUtilities.CreateErrorResult(msg);
            badMethod.StatusCode = StatusCodes.Status405MethodNotAllowed;

            return badMethod;
        }
    }
}
