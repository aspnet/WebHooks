// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebHooks.Filters;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.WebHooks
{
    // ??? Should this also implement IAntiforgeryPolicy to disable antiforgery token verification? Requires a
    // ??? Microsoft.AspNetCore.Mvc.ViewFeatures reference.
    // ??? Should this also implement IDisableCorsAttribute to disable CORS? Requires a Microsoft.AspNetCore.Cors
    // ??? reference.
    /// <summary>
    /// Base for <see cref="Attribute"/>s indicating the associated action is a WebHooks endpoint. Specifies the
    /// required (in most cases) <see cref="ReceiverName"/> and optional <see cref="Id"/>. Also adds a
    /// <see cref="WebHookReceiverExistsFilter"/> for the action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class WebHookAttribute : Attribute, IAllowAnonymous, IFilterFactory
    {
        private string _id;

        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="WebHookAttribute"/> indicating the associated action is a WebHooks
        /// endpoint for all enabled receivers.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName(string receiverName, string id, string[] event, TData data)
        /// </code>
        /// or the subset of parameters required. <c>TData</c> must be compatible with expected requests.
        /// </para>
        /// <para>This constructor should usually be used at most once in a WebHooks application.</para>
        /// <para>
        /// The default route <see cref="IRouteTemplateProvider.Name"/> is
        /// <see cref="WebHookReceiverRouteNames.ReceiverRouteName"/>.
        /// </para>
        /// </summary>
        protected WebHookAttribute()
        {
        }

        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="WebHookAttribute"/> indicating the associated action is a WebHooks
        /// endpoint for the given <paramref name="receiverName"/>.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName(string id, string[] event, TData data)
        /// </code>
        /// or include the subset of parameters required. <c>TData</c> must be compatible with expected requests.
        /// </para>
        /// <para>
        /// This constructor should usually be used at most once per <paramref name="receiverName"/> name in a WebHooks
        /// application.
        /// </para>
        /// <para>The default route <see cref="IRouteTemplateProvider.Name"/> is <c>null</c>.</para>
        /// </summary>
        /// <param name="receiverName">The name of an available <see cref="IWebHookReceiver"/>.</param>
        protected WebHookAttribute(string receiverName)
        {
            if (string.IsNullOrEmpty(receiverName))
            {
                throw new ArgumentException(Resources.General_ArgumentCannotBeNullOrEmpty, nameof(receiverName));
            }

            ReceiverName = receiverName;
        }

        /// <summary>
        /// Gets the name of an available <see cref="IWebHookReceiver"/>.
        /// </summary>
        public string ReceiverName { get; }

        /// <summary>
        /// Gets or sets the id of the configuration this action accepts.
        /// </summary>
        /// <value>
        /// Default value is <c>null</c>, indicating this action accepts all requests for this
        /// <see cref="ReceiverName"/>.
        /// </value>
        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.General_ArgumentCannotBeNullOrEmpty, nameof(value));
                }

                _id = value;
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Allow the <see cref="WebHookReceiverExistsFilter"/> service's registration to determine its lifetime.
        /// </remarks>
        bool IFilterFactory.IsReusable => false;

        /// <inheritdoc />
        IFilterMetadata IFilterFactory.CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var filter = serviceProvider.GetRequiredService<WebHookReceiverExistsFilter>();
            return filter;
        }
    }
}