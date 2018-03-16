// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebHooks.Filters;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// <para>
    /// An <see cref="Attribute"/> indicating the associated action is a Stripe WebHook endpoint. Specifies the
    /// optional <see cref="WebHookAttribute.Id"/>. Also adds a <see cref="WebHookReceiverExistsFilter"/> and a
    /// <see cref="ModelStateInvalidFilter"/> (unless <see cref="ApiBehaviorOptions.SuppressModelStateInvalidFilter"/>
    /// is <see langword="true"/>) for the action.
    /// </para>
    /// <para>
    /// The signature of the action should be:
    /// <code>
    /// Task{IActionResult} ActionName(string id, string @event, string notificationId, TData data)
    /// </code>
    /// or include the subset of parameters required. <c>TData</c> must be compatible with expected requests e.g.
    /// <see cref="Newtonsoft.Json.Linq.JObject"/> or <see cref="StripeEvent"/>.
    /// </para>
    /// <para>
    /// An example Stripe WebHook URI is '<c>https://{host}/api/webhooks/incoming/stripe/{id}</c>'. See
    /// <see href="https://stripe.com/docs/webhooks"/> for additional details about Stripe WebHook requests. See
    /// <see href="https://stripe.com/docs/connect/webhooks"/> for additional details about Stripe Connect WebHook
    /// requests. And, see <see href="https://stripe.com/docs/api/dotnet#events"/> for additional details about Stripe
    /// WebHook request payloads.
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
    /// <see cref="StripeWebHookAttribute"/> should be used at most once per <see cref="WebHookAttribute.Id"/> in a
    /// WebHook application.
    /// </para>
    /// </remarks>
    public class StripeWebHookAttribute : WebHookAttribute
    {
        /// <summary>
        /// Instantiates a new <see cref="StripeWebHookAttribute"/> indicating the associated action is a Stripe
        /// WebHook endpoint.
        /// </summary>
        public StripeWebHookAttribute()
            : base(StripeConstants.ReceiverName)
        {
        }
    }
}
