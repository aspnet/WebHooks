// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    /// <summary>
    /// <para>
    /// Metadata describing the request body type an action expects. If used, must be implemented in a
    /// protocol-specific <see cref="WebHookAttribute"/> subclass. For metadata services, see
    /// <see cref="IWebHookBodyTypeMetadataService"/>.
    /// </para>
    /// <para>
    /// <see cref="ApplicationModels.WebHookModelBindingProvider"/> sets <see cref="Mvc.ModelBinding.BindingInfo"/>
    /// properties based on this metadata or <see cref="IWebHookBodyTypeMetadataService"/> and
    /// <see cref="IWebHookBindingMetadata"/>. This metadata, if available, overrides the
    /// <see cref="IWebHookBodyTypeMetadataService"/> for actions that support a single receiver.
    /// </para>
    /// </summary>
    public interface IWebHookBodyTypeMetadata : IWebHookMetadata
    {
        /// <summary>
        /// Gets the <see cref="WebHookBodyType"/> this action expects.
        /// </summary>
        /// <value>
        /// Must have just one <see cref="WebHookBodyType"/> flag set or be <see cref="WebHookBodyType.All"/>.
        /// Other combinations of flags are not valid. In this context, <see cref="WebHookBodyType.All"/> means a
        /// <c>data</c> parameter is not expected and, if such a parameter exists, it requires no additional
        /// <see cref="Mvc.ModelBinding.BindingInfo"/>.
        /// </value>
        WebHookBodyType BodyType { get; }
    }
}
