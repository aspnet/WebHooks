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
    /// An <see cref="IResourceFilter"/> that short-circuits GitHub ping actions.
    /// </summary>
    public class GitHubWebHookVerifyActionFilter : WebHookReceiver, IResourceFilter
    {
        /// <summary>
        /// Instantiates a new <see cref="GitHubWebHookVerifyActionFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="Logger"/>.
        /// </param>
        /// <param name="receiverConfig">The <see cref="IWebHookReceiverConfig"/>.</param>
        public GitHubWebHookVerifyActionFilter(ILoggerFactory loggerFactory, IWebHookReceiverConfig receiverConfig)
            : base(loggerFactory, receiverConfig)
        {
        }

        /// <inheritdoc />
        public override string Name => GitHubWebHookConstants.ReceiverName;

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
                // Pick out action from headers. Short-circuit further processing if not found.
                var headers = context.HttpContext.Request.Headers;

                // ??? Is GetCommaSeparatedValues() overkill? Unclear GitHub will ever send multiple actions.
                var actions = headers.GetCommaSeparatedValues(GitHubWebHookConstants.EventHeaderName);
                if (actions.Length == 0)
                {
                    Logger.LogError(
                        0,
                        "The WebHook request must contain a '{HeaderName}' HTTP header indicating the type of event.",
                        GitHubWebHookConstants.EventHeaderName);

                    var msg = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Receiver_NoEvent,
                        GitHubWebHookConstants.EventHeaderName);
                    var noHeader = WebHookResultUtilities.CreateErrorResult(msg);

                    context.Result = noHeader;
                    return;
                }

                // If this is a ping request, short-circuit further processing.
                if (string.Equals(actions[0], GitHubWebHookConstants.PingEvent, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(1, "Received a GitHub Ping Event -- ignoring.");

                    context.Result = new OkResult();
                    return;
                }
            }
        }
    }
}
