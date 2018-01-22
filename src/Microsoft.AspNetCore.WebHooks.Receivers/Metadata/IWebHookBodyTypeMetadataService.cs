// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    /// <summary>
    /// Metadata describing the request body type a receiver requires. Implemented in a <see cref="IWebHookMetadata"/>
    /// service for all receivers.
    /// </summary>
    /// <remarks>
    /// <see cref="ApplicationModels.WebHookModelBindingProvider"/> sets <see cref="Mvc.ModelBinding.BindingInfo"/>
    /// properties based on this metadata or <see cref="IWebHookBodyTypeMetadata"/> and
    /// <see cref="IWebHookBindingMetadata"/>. <see cref="Filters.WebHookVerifyBodyTypeFilter"/> confirms the request
    /// body type based on this metadata. <see cref="Filters.WebHookEventMapperFilter"/> uses this metadata to decide
    /// how to parse the request body and how to interpret
    /// <see cref="IWebHookEventFromBodyMetadata.BodyPropertyPath"/>.
    /// </remarks>
    public interface IWebHookBodyTypeMetadataService : IWebHookBodyTypeMetadata, IWebHookReceiver
    {
        /// <summary>
        /// Gets the <see cref="WebHookBodyType"/> this receiver requires.
        /// </summary>
        /// <value>
        /// <c>0</c> is not valid. Otherwise, any combination of defined flags is supported. Most receivers set a
        /// single flag because they require a single body type.
        /// </value>
        new WebHookBodyType BodyType { get; }
    }
}
