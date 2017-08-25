// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> to constrain the accepted HTTP methods. Most receiver projects should
    /// register a subclass. If <see cref="AllowGet"/> is <c>true</c>, the receiver project should register another
    /// <see cref="IResourceFilter"/> which always short-circuits GET requests.
    /// </summary>
    /// <remarks>
    /// The separate <see cref="IResourceFilter"/> implementation avoids multiple actions in user projects.
    /// </remarks>
    public abstract class WebHookVerifyMethodFilter : WebHookReceiver, IResourceFilter
    {
        protected WebHookVerifyMethodFilter(ILoggerFactory loggerFactory, IWebHookReceiverConfig receiverConfig)
            : this(loggerFactory, receiverConfig, allowGet: false)
        {
        }

        protected WebHookVerifyMethodFilter(
            ILoggerFactory loggerFactory,
            IWebHookReceiverConfig receiverConfig,
            bool allowGet)
            : base(loggerFactory, receiverConfig)
        {
            AllowGet = allowGet;
        }

        public bool AllowGet { get; set; }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
