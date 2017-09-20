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
    /// An <see cref="IResourceFilter"/> to allow only POST WebHook requests. To support GET or HEAD requests the
    /// receiver project should register an earlier <see cref="IResourceFilter"/> which always short-circuits such
    /// requests.
    /// </summary>
    /// <remarks>
    /// Done as an <see cref="IResourceFilter"/> implementation and not an
    /// <see cref="Mvc.ActionConstraints.IActionConstraintMetadata"/> because GET and HEAD requests (often pings or
    /// simple verifications) are never of interest in user code.
    /// </remarks>
    public class WebHookVerifyMethodFilter : IResourceFilter
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifyMethodFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="receiverConfig">The <see cref="IWebHookReceiverConfig"/>.</param>
        public WebHookVerifyMethodFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WebHookVerifyMethodFilter>();
        }

        /// <summary>
        /// Gets the <see cref="IOrderedFilter.Order"/> recommended for all <see cref="WebHookVerifyMethodFilter"/>
        /// instances. The recommended filter sequence is
        /// <list type="number">
        /// <item><description>
        /// Confirm signature or <c>code</c> query parameter (in a <see cref="WebHookSecurityFilter"/> subclass).
        /// </description></item>
        /// <item><description>
        /// Confirm required headers and query parameters are provided (in
        /// <see cref="WebHookVerifyRequiredValueFilter"/>).
        /// </description></item>
        /// <item><description>Short-circuit GET or HEAD requests, if receiver supports either.</description></item>
        /// <item><description>Confirm it's a POST request (in this filter).</description></item>
        /// <item><description>Confirm body type (in <see cref="WebHookVerifyBodyTypeFilter"/>).</description></item>
        /// <item><description>
        /// Short-circuit ping requests, if not done in #3 for this receiver (in
        /// <see cref="WebHookPingResponseFilter"/>).
        /// </description></item>
        /// </list>
        /// </summary>
        public static int Order => WebHookVerifyRequiredValueFilter.Order + 10;

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;
            if (context.RouteData.TryGetReceiverName(out var receiverName) &&
                !HttpMethods.IsPost(request.Method))
            {
                // Log about the issue and short-circuit remainder of the pipeline.
                context.Result = CreateBadMethodResult(request);
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }

        private IActionResult CreateBadMethodResult(HttpRequest request)
        {
            _logger.LogError(
                0,
                "The HTTP '{RequestMethod}' method is not supported by the '{ReceiverType}' WebHook receiver.",
                request.Method,
                GetType().Name);

            var message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.VerifyMethod_BadMethod,
                request.Method,
                GetType().Name);

            // ??? Should we instead provide CreateErrorResult(...) overloads with `int statusCode` parameters?
            var badMethod = WebHookResultUtilities.CreateErrorResult(message);
            badMethod.StatusCode = StatusCodes.Status405MethodNotAllowed;

            return badMethod;
        }
    }
}
