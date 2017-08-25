// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Well-known names used in GitHub receivers and handlers.
    /// </summary>
    public static class GitHubWebHookConstants
    {
        /// <summary>
        /// Gets the name of the GitHub WebHook receiver.
        /// </summary>
        public static string ReceiverName => "github";

        /// <summary>
        /// Gets the name of the header containing the GitHub action e.g. <c>ping</c> or <c>push</c>.
        /// </summary>
        /// <remarks>??? Should this be a const field to allow use in [FromHeader] attributes?</remarks>
        public static string EventHeaderName => "X-Github-Event";

        /// <summary>
        /// Gets the name of the GitHub ping event.
        /// </summary>
        public static string PingEvent => "ping";
    }
}
