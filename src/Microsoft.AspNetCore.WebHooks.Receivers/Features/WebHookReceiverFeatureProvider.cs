// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.WebHooks.Receivers.Features
{
    /// <summary>
    /// Discovers controllers from a list of <see cref="ApplicationPart"/> instances.
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
                    if (IsReceiver(type) && !feature.Receivers.Contains(type))
                    {
                        feature.Receivers.Add(type);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a given <paramref name="typeInfo"/> is a receiver.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/> candidate.</param>
        /// <returns><code>true</code> if the type is a receiver; otherwise <code>false</code>.</returns>
        protected virtual bool IsReceiver(TypeInfo typeInfo)
        {
            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            // ??? Okay that next two checks are a bit more stringent than TypeUtilities.IsType<T>()'s IsVisible check?
            // We only consider public top-level classes as receivers. IsPublic returns false for nested
            // classes, regardless of visibility modifiers
            if (!typeInfo.IsPublic)
            {
                return false;
            }

            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            return typeof(IWebHookReceiver).GetTypeInfo().IsAssignableFrom(typeInfo);
        }
    }
}