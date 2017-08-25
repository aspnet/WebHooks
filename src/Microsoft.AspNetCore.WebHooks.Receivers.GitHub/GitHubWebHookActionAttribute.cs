// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// An <see cref="Attribute"/> indicating the associated action is a GitHub WebHooks endpoint. Configures routing
    /// and adds a <see cref="WebHookApplicableFilter"/> for the action. Also specifies the supported request content
    /// types (<c>application/json</c> and <c>text/json</c>).
    /// </summary>
    public class GitHubWebHookActionAttribute : WebHookActionAttribute
    {
        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="GitHubWebHookActionAttribute"/> indicating the associated action is a GitHub
        /// WebHooks endpoint.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName([FromRoute] string id = "", ...)
        /// </code>
        /// or,
        /// <code>
        /// Task{IActionResult} ActionName([FromRoute] string id = "", [FromHeader(Name="X-Github-Event")] string actions, ...)
        /// </code>
        /// </para>
        /// <para>This constructor should usually be used at most once in a WebHooks application.</para>
        /// <para>The default route <see cref="Name"/> is <c>null</c>.</para>
        /// </summary>
        /// <param name="receiver">The name of an available <see cref="IWebHookReceiver"/>.</param>
        /// <param name="contentType">The first supported content type.</param>
        /// <param name="otherContentTypes">Zero or more additional supported content types.</param>
        public GitHubWebHookActionAttribute()
            : base(
                  receiver: GitHubWebHookConstants.ReceiverName,
                  contentType: "application/json",
                  otherContentTypes: "text/json")
        {
        }
    }
}