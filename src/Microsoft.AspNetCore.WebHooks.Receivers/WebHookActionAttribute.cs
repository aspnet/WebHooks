// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.WebHooks.Filters;
using Microsoft.AspNetCore.WebHooks.Routes;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.WebHooks
{
    // ??? Is the WebHookVerifyMethodFilter's checks sufficient for all cases? Only avoid problems with CORS etc. due
    // ??? to constrained routing and the conditional application of our filters. On the other hand, could implement
    // ??? IActionHttpMethodProvider if that's considered simpler and are fine with 404 (not 405) responses in the
    // ??? various WebHooks protocols.
    // ??? Should this also implement IOrderedFilter? For now, doesn't seem important when we check receivers exist.
    /// <summary>
    /// An <see cref="Attribute"/> indicating the associated action is a WebHooks endpoint. Configures routing and adds
    /// a <see cref="WebHookApplicableFilter"/> for the action. Also specifies the supported request content types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class WebHookActionAttribute
        : ConsumesAttribute, IAllowAnonymous, IRouteTemplateProvider, IRouteValueProvider, IFilterFactory
    {
        private readonly string _receiver;

        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="WebHookActionAttribute"/> indicating the associated action is a WebHooks
        /// endpoint for multiple receivers.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName([FromRoute] string webHookReceiver, [FromRoute] string id = "", ...)
        /// </code>
        /// </para>
        /// <para>This constructor should usually be used at most once in a WebHooks application.</para>
        /// <para>
        /// The default route <see cref="Name"/> is <see cref="WebHookReceiverRouteNames.ReceiversAction"/>.
        /// </para>
        /// </summary>
        /// <param name="contentType">The first supported content type.</param>
        /// <param name="otherContentTypes">Zero or more additional supported content types.</param>
        protected WebHookActionAttribute(string contentType, params string[] otherContentTypes)
            : base(contentType, otherContentTypes)
        {
            Name = WebHookReceiverRouteNames.ReceiversAction;
        }

        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="WebHookActionAttribute"/> indicating the associated action is a WebHooks
        /// endpoint for the given <paramref name="receiver"/>.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName([FromRoute] string id = "", ...)
        /// </code>
        /// or,
        /// <code>
        /// Task{IActionResult} ActionName([FromRoute] string webHookReceiver, [FromRoute] string id = "", ...)
        /// </code>
        /// </para>
        /// <para>
        /// This constructor should usually be used at most once per <paramref name="receiver"/> name in a WebHooks
        /// application.
        /// </para>
        /// <para>The default route <see cref="Name"/> is <c>null</c>.</para>
        /// </summary>
        /// <param name="receiver">The name of an available <see cref="IWebHookReceiver"/>.</param>
        /// <param name="contentType">The first supported content type.</param>
        /// <param name="otherContentTypes">Zero or more additional supported content types.</param>
        protected WebHookActionAttribute(string receiver, string contentType, params string[] otherContentTypes)
            : base(contentType, otherContentTypes)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            _receiver = receiver;
        }

        /// <inheritdoc />
        public int? Order { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        /// <remarks>Template is the same for all WebHook actions to make route values consistent.</remarks>
        public string Template
        {
            get
            {
                if (_receiver == null)
                {
                    return $"/api/webhooks/incoming/{{{WebHookReceiverRouteNames.ReceiverKeyName}}}/{{{WebHookReceiverRouteNames.IdKeyName}?}}";
                }

                return $"/api/webhooks/incoming/[{WebHookReceiverRouteNames.ReceiverKeyName}]/{{{WebHookReceiverRouteNames.IdKeyName}?}}";
            }
        }

        /// <inheritdoc />
        public string RouteKey => _receiver == null ? null : WebHookReceiverRouteNames.ReceiverKeyName;

        /// <inheritdoc />
        public string RouteValue => _receiver;

        /// <inheritdoc />
        /// <remarks>
        /// Allow <see cref="WebHookApplicableFilter"/>'s registration in DI to determine its lifetime.
        /// </remarks>
        public bool IsReusable => false;

        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var filter = serviceProvider.GetRequiredService<WebHookApplicableFilter>();
            return filter;
        }
    }
}