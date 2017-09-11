// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    /// <summary>
    /// An <see cref="IWebHookMetadata"/> service containing metadata about the GitHub receiver.
    /// </summary>
    public class GitHubWebHookMetadata : WebHookMetadata, IWebHookEventMetadata
    {
        /// <summary>
        /// Instantiates a new <see cref="GitHubWebHookMetadata"/>.
        /// </summary>
        protected GitHubWebHookMetadata()
            : base(GitHubWebHookConstants.ReceiverName)
        {
        }

        /// <inheritdoc />
        public string ConstantValue => null;

        /// <inheritdoc />
        public string HeaderName => GitHubWebHookConstants.EventHeaderName;

        /// <inheritdoc />
        public string PingEventName => GitHubWebHookConstants.PingEventName;

        /// <inheritdoc />
        public string QueryParameterKey => null;
    }
}
