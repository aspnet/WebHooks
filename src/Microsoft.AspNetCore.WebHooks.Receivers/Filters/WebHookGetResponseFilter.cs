﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> to short-circuit WebHook GET requests.
    /// </summary>
    public class WebHookGetResponseFilter : WebHookSecurityFilter, IResourceFilter
    {
        private readonly IReadOnlyList<IWebHookSecurityMetadata> _getRequestMetadata;

        /// <summary>
        /// Instantiates a new <see cref="WebHookGetResponseFilter"/> instance.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="IConfiguration"/> used to initialize <see cref="WebHookSecurityFilter.Configuration"/>.
        /// </param>
        /// <param name="hostingEnvironment">
        /// The <see cref="IHostingEnvironment" /> used to initialize
        /// <see cref="WebHookSecurityFilter.HostingEnvironment"/>.
        /// </param>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="WebHookSecurityFilter.Logger"/>.
        /// </param>
        /// <param name="metadata">The collection of <see cref="IWebHookMetadata"/> services.</param>
        public WebHookGetResponseFilter(
            IConfiguration configuration,
            IHostingEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory,
            IEnumerable<IWebHookMetadata> metadata)
            : base(configuration, hostingEnvironment, loggerFactory)
        {
            // No need to keep track of IWebHookSecurityMetadata instances that do not request HTTP GET request
            // handling.
            var codeVerifierMetadata = metadata
                .OfType<IWebHookSecurityMetadata>()
                .Where(item => item.ShortCircuitGetRequests);
            _getRequestMetadata = new List<IWebHookSecurityMetadata>(codeVerifierMetadata);
        }

        /// <summary>
        /// Gets the <see cref="IOrderedFilter.Order"/> recommended for all <see cref="WebHookSecurityFilter"/>
        /// instances. The recommended filter sequence is <list type="number">
        /// <item>
        /// Confirm signature or <c>code</c> query parameter (e.g. in <see cref="WebHookVerifyCodeFilter"/> or a
        /// <see cref="WebHookVerifyBodyContentFilter"/> subclass).
        /// </item>
        /// <item>
        /// Confirm required headers and query parameters are provided (in
        /// <see cref="WebHookVerifyRequiredValueFilter"/>).
        /// </item>
        /// <item>Short-circuit GET or HEAD requests, if receiver supports either (in this filter).</item>
        /// <item>Confirm it's a POST request (in <see cref="WebHookVerifyMethodFilter"/>).</item>
        /// <item>Confirm body type (in <see cref="WebHookVerifyBodyTypeFilter"/>).</item>
        /// <item>
        /// Short-circuit ping requests, if not done in this filter for this receiver (in
        /// <see cref="WebHookPingResponseFilter"/>).
        /// </item>
        /// </list>
        /// </summary>
        public new static int Order => WebHookVerifyRequiredValueFilter.Order + 10;

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new System.ArgumentNullException(nameof(context));
            }

            var routeData = context.RouteData;
            if (routeData.TryGetWebHookReceiverName(out var receiverName) &&
                HttpMethods.IsGet(context.HttpContext.Request.Method))
            {
                var getRequestMetadata = _getRequestMetadata
                    .FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (getRequestMetadata != null)
                {
                    var getMetadata = getRequestMetadata.WebHookGetRequest;
                    if (getMetadata == null)
                    {
                        // Simple case. Earlier filters likely did all necessary verification.
                        context.Result = new OkResult();
                        return;
                    }

                    var request = context.HttpContext.Request;
                    context.Result = GetChallengeResponse(getMetadata, receiverName, request, routeData);
                }
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }

        private IActionResult GetChallengeResponse(
            WebHookGetRequest getMetadata,
            string receiverName,
            HttpRequest request,
            RouteData routeData)
        {
            // 1. Verify that we have the secret as an app setting.
            var secretKey = GetSecretKey(
                receiverName,
                routeData,
                getMetadata.SecretKeyMinLength,
                getMetadata.SecretKeyMaxLength);
            if (secretKey == null)
            {
                return new NotFoundResult();
            }

            // 2. Get the 'challenge' parameter from the request URI.
            var challenge = request.Query[getMetadata.ChallengeQueryParameterName];
            if (StringValues.IsNullOrEmpty(challenge))
            {
                Logger.LogError(
                    400,
                    "The WebHook verification request must contain a '{ParameterName}' query parameter.",
                    getMetadata.ChallengeQueryParameterName);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.General_MissingQueryParameter,
                    getMetadata.ChallengeQueryParameterName);
                var noChallenge = WebHookResultUtilities.CreateErrorResult(message);

                return noChallenge;
            }

            // 3. Echo the challenge back to the caller.
            return new ContentResult
            {
                Content = challenge,
            };
        }
    }
}
