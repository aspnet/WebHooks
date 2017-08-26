﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstract <see cref="IWebHookHandler" /> implementation which can be used to enqueue
    /// WebHooks for processing outside their immediate HTTP request/response context. This can for example
    /// be used to process WebHooks by a separate agent or at another time. It can also be used for WebHooks
    /// where the processing take longer than permitted by the immediate HTTP request/response context.
    /// </summary>
    public abstract class WebHookQueueHandler : WebHookHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookQueueHandler"/> class with a default <see cref="WebHookHandler.Order"/> of 50
        /// and by default accepts WebHooks from all receivers. To limit which receiver this <see cref="IWebHookHandler"/>
        /// will receive WebHook requests from, set the <see cref="WebHookHandler.Receiver"/> property to the name of that receiver.
        /// </summary>
        protected WebHookQueueHandler(ILoggerFactory loggerFactory)
            : base()
        {
            Logger = loggerFactory.CreateLogger(GetType());
        }

        protected ILogger Logger { get; }

        /// <inheritdoc />
        public override async Task ExecuteAsync(string receiver, WebHookHandlerContext context)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                var queueContext = new WebHookQueueContext(receiver, context);
                context.Result = await EnqueueAsync(queueContext);
            }
            catch (Exception ex)
            {
                Logger.LogError(500, ex, "Could not enqueue WebHook.");

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.QueueHandler_EnqueueError, ex.Message);
                context.Result = CreateErrorResult(StatusCodes.Status500InternalServerError, msg);
            }
        }

        /// <summary>
        /// Enqueues an incoming WebHook for processing outside its immediate HTTP request/response context.
        /// Any exception thrown will result in an HTTP error response being returned to the party generating
        /// the WebHook.
        /// </summary>
        /// <param name="context">The <see cref="WebHookQueueContext"/> for the WebHook to be enqueued.</param>
        public abstract Task<IActionResult> EnqueueAsync(WebHookQueueContext context);
    }
}
