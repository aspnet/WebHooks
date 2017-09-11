// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.WebHooks.Routing
{
    /// <summary>
    /// An <see cref="IActionConstraintFactory"/> implementation that returns
    /// <see cref="WebHookMultipleEventMapperConstraint"/> instances from dependency injection.
    /// </summary>
    public class WebHookMultipleEventMapperConstraintFactory : IActionConstraintFactory
    {
        /// <inheritdoc />
        /// <remarks>
        /// Allow the <see cref="WebHookMultipleEventMapperConstraint"/> service's registration to determine its
        /// lifetime.
        /// </remarks>
        public bool IsReusable => false;

        /// <inheritdoc />
        public IActionConstraint CreateInstance(IServiceProvider services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var constraint = services.GetRequiredService<WebHookMultipleEventMapperConstraint>();
            return constraint;
        }
    }
}
