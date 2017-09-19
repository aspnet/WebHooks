// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> that verifies the <c>code</c> query parameter. Short-circuits the request if
    /// the <c>code</c> query parameter is missing or does not match the receiver's configuration. Also confirms the
    /// request URI uses the <c>HTTPS</c> scheme.
    /// </summary>
    public class WebHookVerifyCodeFilter : WebHookSecurityFilter, IAsyncResourceFilter
    {
        // Information about the 'code' URI parameter
        internal const int CodeMinLength = 32;
        internal const int CodeMaxLength = 128;
        internal const string CodeQueryParameter = "code";

        private readonly IReadOnlyList<IWebHookSecurityMetadata> _securityMetadata;

        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifyCodeFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="WebHookSecurityFilter.Logger"/>.
        /// </param>
        /// <param name="metadata">The collection of <see cref="IWebHookMetadata"/> services.</param>
        /// <param name="receiverConfig">
        /// The <see cref="IWebHookReceiverConfig"/> used to initialize
        /// <see cref="WebHookSecurityFilter.Configuration"/> and <see cref="WebHookSecurityFilter.ReceiverConfig"/>.
        /// </param>
        public WebHookVerifyCodeFilter(
            ILoggerFactory loggerFactory,
            IEnumerable<IWebHookMetadata> metadata,
            IWebHookReceiverConfig receiverConfig)
            : base(loggerFactory, receiverConfig)
        {
            _securityMetadata = new List<IWebHookSecurityMetadata>(metadata.OfType<IWebHookSecurityMetadata>());
        }

        /// <inheritdoc />
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var routeData = context.RouteData;
            if (routeData.TryGetReceiverName(out var receiverName))
            {
                var securityMetadata = _securityMetadata
                    .FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (securityMetadata != null && securityMetadata.VerifyCodeParameter)
                {
                    routeData.TryGetReceiverId(out var id);
                    var result = await EnsureValidCode(context.HttpContext.Request, receiverName, id);
                    if (result != null)
                    {
                        context.Result = result;
                        return;
                    }
                }
            }

            await next();
        }

        /// <summary>
        /// For WebHook providers with insufficient security considerations, the receiver can require that the WebHook
        /// URI must be an <c>https</c> URI and contain a 'code' query parameter with a value configured for that
        /// particular <paramref name="id"/>. A sample WebHook URI is
        /// '<c>https://&lt;host&gt;/api/webhooks/incoming/&lt;receiver&gt;?code=83699ec7c1d794c0c780e49a5c72972590571fd8</c>'.
        /// The 'code' parameter must be between 32 and 128 characters long.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="receiverName">The name of an available <see cref="IWebHookReceiver"/>.</param>
        /// <param name="id">
        /// A (potentially empty) ID of a particular configuration for this <see cref="WebHookVerifyCodeFilter"/>. This
        /// allows an <see cref="WebHookVerifyCodeFilter"/> to support multiple senders with individual configurations.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides <c>null</c> in the success case. When a check fails,
        /// provides an <see cref="IActionResult"/> that when executed will produce a response containing details about
        /// the problem.
        /// </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Response is disposed by Web API.")]
        protected virtual async Task<IActionResult> EnsureValidCode(
            HttpRequest request,
            string receiverName,
            string id)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var result = EnsureSecureConnection(request);
            if (result != null)
            {
                return result;
            }

            var code = request.Query[CodeQueryParameter];
            if (StringValues.IsNullOrEmpty(code))
            {
                Logger.LogError(
                    400,
                    "The WebHook verification request must contain a '{ParameterName}' query parameter.",
                    CodeQueryParameter);

                var message = string.Format(CultureInfo.CurrentCulture, Resources.Receiver_NoCode, CodeQueryParameter);
                var noCode = WebHookResultUtilities.CreateErrorResult(message);

                return noCode;
            }

            var secretKey = await GetReceiverConfig(request, receiverName, id, CodeMinLength, CodeMaxLength);
            if (!SecretEqual(code, secretKey))
            {
                Logger.LogError(
                    401,
                    "The '{ParameterName}' query parameter provided in the HTTP request did not match the expected value.",
                    CodeQueryParameter);

                var message = string.Format(CultureInfo.CurrentCulture, Resources.Receiver_BadCode, CodeQueryParameter);
                var invalidCode = WebHookResultUtilities.CreateErrorResult(message);

                return invalidCode;
            }

            return null;
        }
    }
}
