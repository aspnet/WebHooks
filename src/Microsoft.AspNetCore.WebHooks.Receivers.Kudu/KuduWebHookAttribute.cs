// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// An <see cref="System.Attribute"/> indicating the associated action is a Kudu WebHooks endpoint. Specifies the
    /// optional <see cref="WebHookAttribute.Id"/>. Also adds a <see cref="Filters.WebHookReceiverExistsFilter"/> for
    /// the action.
    /// </summary>
    public class KuduWebHookAttribute : WebHookAttribute
    {
        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="KuduWebHookAttribute"/> indicating the associated action is a Kudu
        /// WebHooks endpoint.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName(string id, TData data)
        /// </code>
        /// or include the subset of parameters required. <c>TData</c> must be compatible with expected requests e.g.
        /// <see cref="Newtonsoft.Json.Linq.JObject"/> or <see cref="KuduNotification"/>.
        /// </para>
        /// <para>This constructor should usually be used at most once in a WebHook application.</para>
        /// <para>The default route <see cref="IRouteTemplateProvider.Name"/> is <c>null</c>.</para>
        /// </summary>
        public KuduWebHookAttribute()
            : base(KuduConstants.ReceiverName)
        {
        }
    }
}