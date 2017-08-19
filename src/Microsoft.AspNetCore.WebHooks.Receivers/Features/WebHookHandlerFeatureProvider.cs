// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.WebHooks.Receivers.Features
{
    // Modeled after ControllerFeatureProvider in ASP.NET Core MVC.
    /// <summary>
    /// Discovers handlers from a list of <see cref="ApplicationPart"/> instances.
    /// </summary>
    public class WebHookHandlerFeatureProvider : IApplicationFeatureProvider<WebHookHandlerFeature>
    {
        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, WebHookHandlerFeature feature)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }
            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            foreach (var part in parts.OfType<IApplicationPartTypeProvider>())
            {
                foreach (var type in part.Types)
                {
                    if (TypeUtilities.IsType<IWebHookHandler>(type) && !feature.Handlers.Contains(type))
                    {
                        feature.Handlers.Add(type);
                    }
                }
            }
        }
    }
}