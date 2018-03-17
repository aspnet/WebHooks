// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebHooks.Filters;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// <para>
    /// An <see cref="Attribute"/> indicating the associated action is a Slack WebHook endpoint. Specifies the optional
    /// <see cref="WebHookAttribute.Id"/>. Adds a <see cref="WebHookReceiverExistsFilter"/> and a
    /// <see cref="ModelStateInvalidFilter"/> (unless <see cref="ApiBehaviorOptions.SuppressModelStateInvalidFilter"/>
    /// is <see langword="true"/>) for the action. Also delegates its <see cref="IResultFilter"/> and
    /// <see cref="IApiResponseMetadataProvider"/> implementations to a <see cref="ProducesAttribute"/>, indicating the
    /// action produces JSON-formatted responses.
    /// </para>
    /// <para>
    /// The signature of the action should be:
    /// <code>
    /// Task{TResult} ActionName(string id, string @event, string subtext, TData data)
    /// </code>
    /// or include the subset of parameters required. <c>TData</c> must be compatible with expected requests e.g.
    /// <see cref="Http.IFormCollection"/>. <c>TResult</c> may be <see cref="SlackResponse"/>,
    /// <see cref="SlackSlashResponse"/>, or an <see cref="IActionResult"/> implementation.
    /// </para>
    /// <para>
    /// An example Slack WebHook URI is '<c>https://{host}/api/webhooks/incoming/slack/{id}</c>'.
    /// See <see href="https://api.slack.com/custom-integrations/outgoing-webhooks"/> for additional details about
    /// Slack WebHook requests.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the application enables CORS in general (see the <c>Microsoft.AspNetCore.Cors</c> package), apply
    /// <c>DisableCorsAttribute</c> to this action. If the application depends on the
    /// <c>Microsoft.AspNetCore.Mvc.ViewFeatures</c> package, apply <c>IgnoreAntiforgeryTokenAttribute</c> to this
    /// action.
    /// </para>
    /// <para>
    /// <see cref="SlackWebHookAttribute"/> should be used at most once per <see cref="WebHookAttribute.Id"/> in a
    /// WebHook application.
    /// </para>
    /// <para>
    /// Implements <see cref="IApiResponseMetadataProvider"/> to provide information to a <see cref="FormatFilter"/>.
    /// Implements <see cref="IResultFilter"/> for the odd case where no <see cref="FormatFilter"/> applies.
    /// </para>
    /// </remarks>
    public class SlackWebHookAttribute : WebHookAttribute, IResultFilter, IApiResponseMetadataProvider
    {
        private static readonly ProducesAttribute Produces = new ProducesAttribute("application/json");

        /// <summary>
        /// Instantiates a new <see cref="SlackWebHookAttribute"/> indicating the associated action is a Slack WebHook
        /// endpoint.
        /// </summary>
        public SlackWebHookAttribute()
            : base(SlackConstants.ReceiverName)
        {
        }

        /// <inheritdoc />
        Type IApiResponseMetadataProvider.Type => Produces.Type;

        /// <inheritdoc />
        int IApiResponseMetadataProvider.StatusCode => Produces.StatusCode;

        /// <inheritdoc />
        void IApiResponseMetadataProvider.SetContentTypes(MediaTypeCollection contentTypes)
            => Produces.SetContentTypes(contentTypes);

        /// <inheritdoc />
        void IResultFilter.OnResultExecuting(ResultExecutingContext context) => Produces.OnResultExecuting(context);

        /// <inheritdoc />
        void IResultFilter.OnResultExecuted(ResultExecutedContext context) => Produces.OnResultExecuted(context);
    }
}
