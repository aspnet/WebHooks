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
    /// Discovers receivers from a list of <see cref="ApplicationPart"/> instances.
    /// </summary>
    public class WebHookReceiverFeatureProvider : IApplicationFeatureProvider<WebHookReceiverFeature>
    {
        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, WebHookReceiverFeature feature)
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
                    if (TypeUtilities.IsType<IWebHookReceiver>(type) && !feature.Receivers.Contains(type))
                    {
                        feature.Receivers.Add(type);
                    }
                }
            }
        }
    }
}