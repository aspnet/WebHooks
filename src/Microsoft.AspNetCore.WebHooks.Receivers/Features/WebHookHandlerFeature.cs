// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.WebHooks.Receivers.Features
{
    // ??? Remove IMvcBuilder.PartManager mention? WebHookServiceCollectionExtensions.AddWebHooks() overloads do not expose an IMvcBuilder.
    /// <summary>
    /// The list of handler types in a WebHooks application. The <see cref="WebHookHandlerFeature"/> can be populated
    /// using the <see cref="ApplicationPartManager"/> that is available during startup at <see cref="IMvcBuilder.PartManager"/>
    /// and <see cref="IMvcCoreBuilder.PartManager"/> or at a later stage by requiring the <see cref="ApplicationPartManager"/>
    /// as a dependency in a component.
    /// </summary>
    public class WebHookHandlerFeature
    {
        /// <summary>
        /// Gets the list of receiver types in an WebHooks application.
        /// </summary>
        public IList<TypeInfo> Handlers { get; } = new List<TypeInfo>();
    }
}
