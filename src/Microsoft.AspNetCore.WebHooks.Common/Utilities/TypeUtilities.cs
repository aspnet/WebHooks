// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNetCore.WebHooks.Utilities
{
    /// <summary>
    /// Provides various <see cref="System.Type"/> related utilities.
    /// </summary>
    public static class TypeUtilities
    {
        // ??? Okay that this is a bit more stringent than Microsoft.AspNet.WebHooks version i.e. this version
        // ??? disallows nested classes and open generic types?
        // Modeled after ControllerFeatureProvider.IsController() in ASP.NET Core MVC.
        /// <summary>
        /// Checks whether <paramref name="type"/> is a visible, non-abstract class of type <typeparamref name="T"/> or
        /// derived from type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to test against.</typeparam>
        /// <param name="type">The <see cref="TypeInfo"/> to test.</param>
        /// <returns><c>true</c> if the type is of type <typeparamref name="T"/>.</returns>
        public static bool IsType<T>(TypeInfo type)
        {
            if (type == null)
            {
                return false;
            }

            if (!type.IsClass)
            {
                return false;
            }

            if (type.IsAbstract)
            {
                return false;
            }

            // We only consider public top-level classes. IsPublic returns false for nested
            // classes, regardless of visibility modifiers
            if (!type.IsPublic)
            {
                return false;
            }

            if (type.ContainsGenericParameters)
            {
                return false;
            }

            return typeof(T).GetTypeInfo().IsAssignableFrom(type);
        }
    }
}