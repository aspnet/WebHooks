// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// An <see cref="Attribute"/> indicating the associated action is an Azure Alert WebHooks endpoint. Specifies the
    /// optional <see cref="WebHookActionAttributeBase.Id"/>. Also adds a
    /// <see cref="Filters.WebHookReceiverExistsFilter"/> for the action.
    /// </summary>
    public class AzureAlertActionAttribute : WebHookActionAttributeBase
    {
        /// <summary>
        /// <para>
        /// Instantiates a new <see cref="AzureAlertActionAttribute"/> indicating the associated action is an Azure
        /// Alert WebHooks endpoint.
        /// </para>
        /// <para>The signature of the action should be:
        /// <code>
        /// Task{IActionResult} ActionName(string id, TData data)
        /// </code>
        /// or include the subset of parameters required. <c>TData</c> must be compatible with expected requests e.g.
        /// <see cref="Newtonsoft.Json.Linq.JObject"/> or <see cref="AzureAlertNotification"/>.
        /// </para>
        /// <para>This constructor should usually be used at most once in a WebHook application.</para>
        /// <para>The default route <see cref="IRouteTemplateProvider.Name"/> is <c>null</c>.</para>
        /// </summary>
        public AzureAlertActionAttribute()
            : base(AzureAlertConstants.ReceiverName)
        {
        }
    }
}