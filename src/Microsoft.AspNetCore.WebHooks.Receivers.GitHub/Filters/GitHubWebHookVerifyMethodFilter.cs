// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// A <see cref="WebHookVerifyMethodFilter"/> that rejects all non-POST GitHub WebHook requests.
    /// </summary>
    public class GitHubWebHookVerifyMethodFilter : WebHookVerifyMethodFilter
    {
        /// <summary>
        /// Instantiates a new <see cref="GitHubWebHookVerifyMethodFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="Logger"/>.
        /// </param>
        /// <param name="receiverConfig">The <see cref="IWebHookReceiverConfig"/>.</param>
        public GitHubWebHookVerifyMethodFilter(ILoggerFactory loggerFactory, IWebHookReceiverConfig receiverConfig)
            : base(loggerFactory, receiverConfig)
        {
        }

        /// <inheritdoc />
        public override string ReceiverName => GitHubWebHookConstants.ReceiverName;

        /// <inheritdoc />
        public override bool AllowGet => false;
    }
}
