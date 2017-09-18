// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> that confirms the <see cref="Routing.WebHookReceiverExistsConstraint"/> is
    /// configured and ran successfully for this request. Also confirms at least one <see cref="IWebHookReceiver"/>
    /// filter is configured to handle this request. The minimal receiver configuration includes a
    /// <see cref="WebHookReceiverFilter"/> subclass to verify signatures.
    /// </summary>
    public class WebHookReceiverExistsFilter : IResourceFilter
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="WebHookReceiverExistsFilter"/> with the given
        /// <paramref name="loggerFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebHookReceiverExistsFilter(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<WebHookReceiverExistsFilter>();
        }

        /// <summary>
        /// <para>
        /// Confirms the <see cref="Routing.WebHookReceiverExistsConstraint"/> is configured and ran successfully for
        /// this request. Also confirms at least one <see cref="IWebHookReceiver"/> filter is configured to handle this
        /// request.
        /// </para>
        /// <para>
        /// Logs an informational message when both confirmations succeed. If either confirmation fails, sets
        /// <see cref="ResourceExecutingContext.Result"/> to a <see cref="StatusCodeResult"/> with
        /// <see cref="StatusCodeResult.StatusCode"/> set to <see cref="StatusCodes.Status500InternalServerError"/>.
        /// </para>
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.RouteData.TryGetReceiverName(out var receiverName))
            {
                if (!context.RouteData.GetReceiverExists())
                {
                    _logger.LogCritical(
                        0,
                        "Unable to find WebHook routing constraints for {ReceiverName}. Please add the required " +
                        "configuration by calling a receiver-specific method that calls " +
                        "'{CoreInterfaceName}.{MethodName}' in the application startup code. For example, call " +
                        "'{GitHubCoreInterfaceName}.{GitHubMethodName}' to configure the GitHub receiver.",
                        receiverName,
                        nameof(IMvcCoreBuilder),
                        nameof(WebHookMvcCoreBuilderExtensions.AddWebHooks),
                        nameof(IMvcCoreBuilder),
                        "AddGitHubWebHooks");

                    context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    return;
                }

                var found = false;
                for (var i = 0; i < context.Filters.Count; i++)
                {
                    var filter = context.Filters[i];
                    if (filter is IWebHookReceiver receiver && receiver.IsApplicable(receiverName))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // This case is actually more likely a gap in the receiver-specific configuration method.
                    _logger.LogCritical(
                        1,
                        "Unable to find WebHook filters for {ReceiverName}. Please add the required configuration " +
                        "by calling a receiver-specific method that calls '{CoreInterfaceName}.{MethodName}' in the " +
                        "application startup code. For example, call '{GitHubCoreInterfaceName}.{GitHubMethodName}' " +
                        "to configure the GitHub receiver.",
                        receiverName,
                        nameof(IMvcCoreBuilder),
                        nameof(WebHookMvcCoreBuilderExtensions.AddWebHooks),
                        nameof(IMvcCoreBuilder),
                        "AddGitHubWebHooks");

                    context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    return;
                }
            }
            else
            {
                // Routing not configured at all (no template) but the request reached this action.
                _logger.LogCritical(
                    2,
                    "Unable to find WebHook routing information in the request. Please add the required " +
                    "configuration by calling a receiver-specific method that calls " +
                    "'{CoreInterfaceName}.{MethodName}' in the application startup code. For example, call " +
                    "'{GitHubCoreInterfaceName}.{GitHubMethodName}' to configure the GitHub receiver.",
                    nameof(IMvcCoreBuilder),
                    nameof(WebHookMvcCoreBuilderExtensions.AddWebHooks),
                    nameof(IMvcCoreBuilder),
                    "AddGitHubWebHooks");

                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            context.RouteData.TryGetReceiverId(out var id);
            _logger.LogInformation(
                3,
                "Processing incoming WebHook request with receiver '{ReceiverName}' and id '{Id}'.",
                receiverName,
                id);
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }
    }
}
