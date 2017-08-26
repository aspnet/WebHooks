// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.WebHooks.Receivers.Features
{
    /// <summary>
    /// The list of receiver types in a WebHooks application. The <see cref="WebHookReceiverFeature"/> can be populated
    /// using the <see cref="Mvc.ApplicationParts.ApplicationPartManager"/> that is available during startup at
    /// <see cref="Extensions.DependencyInjection.IMvcBuilder.PartManager"/> and
    /// <see cref="Extensions.DependencyInjection.IMvcCoreBuilder.PartManager"/> or at a later stage by requiring the
    /// <see cref="Mvc.ApplicationParts.ApplicationPartManager"/> as a dependency in a component.
    /// </summary>
    public class WebHookReceiverFeature
    {
        /// <summary>
        /// Gets the list of receiver types in an WebHooks application.
        /// </summary>
        public IList<TypeInfo> Receivers { get; } = new List<TypeInfo>();
    }
}
