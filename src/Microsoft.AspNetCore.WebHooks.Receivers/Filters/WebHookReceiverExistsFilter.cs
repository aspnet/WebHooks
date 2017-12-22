﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> that confirms the <see cref="Routing.WebHookReceiverExistsConstraint"/> is
    /// configured and ran successfully for this request. Also confirms either <see cref="IWebHookVerifyCodeMetadata"/>
    /// is applicable or at least one <see cref="IWebHookReceiver"/> filter is configured to handle this request.
    /// The minimal configuration for a receiver without <see cref="IWebHookVerifyCodeMetadata"/> includes a
    /// <see cref="WebHookSecurityFilter"/> subclass to verify signatures or otherwise check secret keys.
    /// </summary>
    public class WebHookReceiverExistsFilter : IResourceFilter
    {
        private readonly IReadOnlyList<IWebHookVerifyCodeMetadata> _codeVerifierMetadata;
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="WebHookReceiverExistsFilter"/> with the given
        /// <paramref name="loggerFactory"/>.
        /// </summary>
        /// <param name="metadata">The collection of <see cref="IWebHookMetadata"/> services.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebHookReceiverExistsFilter(IEnumerable<IWebHookMetadata> metadata, ILoggerFactory loggerFactory)
        {
            _codeVerifierMetadata = metadata.OfType<IWebHookVerifyCodeMetadata>().ToArray();
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

            if (context.RouteData.TryGetWebHookReceiverName(out var receiverName))
            {
                if (!context.RouteData.GetWebHookReceiverExists())
                {
                    _logger.LogCritical(
                        0,
                        "Unable to find WebHook routing constraints for the '{ReceiverName}' receiver. Please add " +
                        "the required configuration by calling a receiver-specific method that calls " +
                        "'{BuilderInterface}.{BuilderMethod}' or '{CoreInterface}.{CoreBuilderMethod}' in the " +
                        "application startup code. For example, call " +
                        "'{GitHubBuilderInterface}.{GitHubBuilderMethod}' to configure a minimal GitHub receiver.",
                        receiverName,
                        nameof(IMvcBuilder),
                        nameof(WebHookMvcBuilderExtensions.AddWebHooks),
                        nameof(IMvcCoreBuilder),
                        nameof(WebHookMvcCoreBuilderExtensions.AddWebHooks),
                        nameof(IMvcCoreBuilder),
                        "AddGitHubWebHooks");

                    context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    return;
                }

                // Check for receiver-specific filters only for receivers that do _not_ use code verification.
                if (!_codeVerifierMetadata.Any(metadata => metadata.IsApplicable(receiverName)))
                {
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
                            "Unable to find WebHook filters for the '{ReceiverName}' receiver. Please add the " +
                            "required configuration by calling a receiver-specific method that calls " +
                            "'{BuilderInterface}.{BuilderMethod}' or '{CoreInterface}.{CoreBuilderMethod}' in the " +
                            "application startup code. For example, call " +
                            "'{GitHubBuilderInterface}.{GitHubBuilderMethod}' to configure a minimal GitHub receiver.",
                            receiverName,
                            nameof(IMvcBuilder),
                            nameof(WebHookMvcBuilderExtensions.AddWebHooks),
                            nameof(IMvcCoreBuilder),
                            nameof(WebHookMvcCoreBuilderExtensions.AddWebHooks),
                            nameof(IMvcCoreBuilder),
                            "AddGitHubWebHooks");

                        context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                        return;
                    }
                }
            }
            else
            {
                // Routing not configured at all (no template) but the request reached this action.
                _logger.LogCritical(
                    2,
                    "Unable to find WebHook routing information in the request. Please add the required " +
                    "configuration by calling a receiver-specific method that calls " +
                    "'{BuilderInterface}.{BuilderMethod}' or '{CoreInterface}.{CoreBuilderMethod}' in the " +
                    "application startup code. For example, call '{GitHubBuilderInterface}.{GitHubBuilderMethod}' " +
                    "to configure a minimal GitHub receiver.",
                    nameof(IMvcBuilder),
                    nameof(WebHookMvcBuilderExtensions.AddWebHooks),
                    nameof(IMvcCoreBuilder),
                    nameof(WebHookMvcCoreBuilderExtensions.AddWebHooks),
                    nameof(IMvcCoreBuilder),
                    "AddGitHubWebHooks");

                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            context.RouteData.TryGetWebHookReceiverId(out var id);
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
