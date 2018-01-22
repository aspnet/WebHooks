// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    /// <summary>
    /// General description of the required request body for a WebHook controller action.
    /// </summary>
    /// <remarks>The value <c>0</c> is not valid; a bit must be set in any use of this type.</remarks>
    /// <seealso cref="IWebHookBodyTypeMetadata.BodyType"/>
    /// <seealso cref="IWebHookBodyTypeMetadataService.BodyType"/>
    [Flags]
    public enum WebHookBodyType
    {
        /// <summary>
        /// Request must have <c>content-type</c> <c>application/x-www-form-urlencoded</c>. The
        /// <see cref="ApplicationModels.WebHookModelBindingProvider"/> gives a bound <c>data</c> parameter the same
        /// <see cref="Mvc.ModelBinding.BindingInfo"/> as an associated <c>[FromForm]</c> attribute when the action or
        /// receiver has this <see cref="WebHookBodyType"/>.
        /// </summary>
        Form = 1,

        /// <summary>
        /// Request must have <c>content-type</c> <c>application/json</c>, <c>application/*+json</c>, <c>text/json</c>,
        /// or a subset. The <see cref="ApplicationModels.WebHookModelBindingProvider"/> gives a bound <c>data</c>
        /// parameter the same <see cref="Mvc.ModelBinding.BindingInfo"/> as an associated <c>[FromBody]</c> attribute
        /// when the action or receiver has this <see cref="WebHookBodyType"/>.
        /// </summary>
        Json = 2,

        /// <summary>
        /// Request must have <c>content-type</c> <c>application/xml</c>, <c>application/*+xml</c>, <c>text/xml</c>,
        /// or a subset. The <see cref="ApplicationModels.WebHookModelBindingProvider"/> gives a bound <c>data</c>
        /// parameter the same <see cref="Mvc.ModelBinding.BindingInfo"/> as an associated <c>[FromBody]</c> attribute
        /// when the action or receiver has this <see cref="WebHookBodyType"/>.
        /// </summary>
        Xml = 4,

        /// <summary>
        /// Request may have any supported <c>content-type</c>. The
        /// <see cref="ApplicationModels.WebHookModelBindingProvider"/> ignores bound <c>data</c> parameters when the
        /// action or receiver has this <see cref="WebHookBodyType"/>.
        /// </summary>
        /// <remarks>
        /// This value is intended for use as <see cref="GeneralWebHookAttribute"/>'s default
        /// <see cref="IWebHookBodyTypeMetadata.BodyType"/>.
        /// </remarks>
        All = Form | Json | Xml,
    }
}
