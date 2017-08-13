﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNet.WebHooks.Diagnostics;
using Microsoft.AspNet.WebHooks.Utilities;

namespace Microsoft.AspNet.WebHooks.Services
{
    /// <summary>
    /// Provides singleton instances of custom WebHook services such as a default
    /// <see cref="IWebHookStore"/> implementation, <see cref="IWebHookManager"/> etc.
    /// If alternative implementations are provided by a Dependency Injection engine then
    /// those instances are used instead.
    /// </summary>
    public static class CustomServices
    {
        private static IEnumerable<IWebHookFilterProvider> _filterProviders;
        private static IWebHookFilterManager _filterManager;
        private static IWebHookStore _store;
        private static IWebHookSender _sender;
        private static IWebHookManager _manager;
        private static IWebHookRegistrationsManager _registrationsManager;
        private static IWebHookUser _user;

        /// <summary>
        /// Gets a default <see cref="IWebHookStore"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <returns>A default <see cref="IWebHookStore"/> instance.</returns>
        public static IWebHookStore GetStore()
        {
            if (_store != null)
            {
                return _store;
            }

            IWebHookStore instance = new MemoryWebHookStore();
            Interlocked.CompareExchange(ref _store, instance, null);
            return _store;
        }

        /// <summary>
        /// Sets a default <see cref="IWebHookStore"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <param name="instance">The <see cref="IWebHookStore"/> to use. If <c>null</c> then a default implementation is used.</param>
        public static void SetStore(IWebHookStore instance)
        {
            _store = instance;
        }

        /// <summary>
        /// Gets a default <see cref="IWebHookUser"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <returns>A default <see cref="IWebHookUser"/> instance.</returns>
        public static IWebHookUser GetUser()
        {
            if (_user != null)
            {
                return _user;
            }

            IWebHookUser instance = new WebHookUser();
            Interlocked.CompareExchange(ref _user, instance, null);
            return _user;
        }

        /// <summary>
        /// Sets a default <see cref="IWebHookUser"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <param name="instance">The <see cref="IWebHookUser"/> to use. If <c>null</c> then a default implementation is used.</param>
        public static void SetUser(IWebHookUser instance)
        {
            _user = instance;
        }

        /// <summary>
        /// Gets the set of <see cref="IWebHookFilterProvider"/> instances discovered by a default 
        /// discovery mechanism which is used if none are registered with the Dependency Injection engine.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> containing the discovered instances.</returns>
        public static IEnumerable<IWebHookFilterProvider> GetFilterProviders()
        {
            if (_filterProviders != null)
            {
                return _filterProviders;
            }

            IAssembliesResolver assembliesResolver = WebHooksConfig.Config.Services.GetAssembliesResolver();
            ICollection<Assembly> assemblies = assembliesResolver.GetAssemblies();
            IEnumerable<IWebHookFilterProvider> instances = TypeUtilities.GetInstances<IWebHookFilterProvider>(assemblies, t => TypeUtilities.IsType<IWebHookFilterProvider>(t));
            Interlocked.CompareExchange(ref _filterProviders, instances, null);
            return _filterProviders;
        }

        /// <summary>
        /// Gets a default <see cref="IWebHookFilterManager"/> implementation which is used if none are registered with the
        /// Dependency Injection engine.
        /// </summary>
        /// <param name="filterProviders">The collection of <see cref="IWebHookFilterProvider"/> instances to use.</param>
        /// <returns>A default <see cref="IWebHookFilterManager"/> instance.</returns>
        public static IWebHookFilterManager GetFilterManager(IEnumerable<IWebHookFilterProvider> filterProviders)
        {
            if (_filterManager != null)
            {
                return _filterManager;
            }

            if (filterProviders == null)
            {
                throw new ArgumentNullException(nameof(filterProviders));
            }

            IWebHookFilterManager instance = new WebHookFilterManager(filterProviders);
            Interlocked.CompareExchange(ref _filterManager, instance, null);
            return _filterManager;
        }

        /// <summary>
        /// Gets a default <see cref="IWebHookSender"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <returns>A default <see cref="IWebHookSender"/> instance.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by AppDomain")]
        public static IWebHookSender GetSender(ILogger logger)
        {
            if (_sender != null)
            {
                return _sender;
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            IWebHookSender instance = new DataflowWebHookSender(logger);
            Interlocked.CompareExchange(ref _sender, instance, null);
            return _sender;
        }

        /// <summary>
        /// Sets a default <see cref="IWebHookSender"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <param name="instance">The <see cref="IWebHookSender"/> to use. If <c>null</c> then a default implementation is used.</param>
        public static void SetSender(IWebHookSender instance)
        {
            _sender = instance;
        }

        /// <summary>
        /// Gets a default <see cref="IWebHookManager"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <returns>A default <see cref="IWebHookManager"/> instance.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by AppDomain")]
        public static IWebHookManager GetManager(IWebHookStore store, IWebHookSender sender, ILogger logger)
        {
            if (_manager != null)
            {
                return _manager;
            }
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            IWebHookManager instance = new WebHookManager(store, sender, logger);
            Interlocked.CompareExchange(ref _manager, instance, null);
            return _manager;
        }

        /// <summary>
        /// Gets a default <see cref="IWebHookRegistrationsManager"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <returns>A default <see cref="IWebHookManager"/> instance.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by AppDomain")]
        public static IWebHookRegistrationsManager GetRegistrationsManager(IWebHookManager manager, IWebHookStore store, IWebHookFilterManager filterManager, IWebHookUser userManager)
        {
            if (_registrationsManager != null)
            {
                return _registrationsManager;
            }
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            if (filterManager == null)
            {
                throw new ArgumentNullException(nameof(filterManager));
            }
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }

            IWebHookRegistrationsManager instance = new WebHookRegistrationsManager(manager, store, filterManager, userManager);
            Interlocked.CompareExchange(ref _registrationsManager, instance, null);
            return _registrationsManager;
        }

        /// <summary>
        /// Sets a default <see cref="IWebHookRegistrationsManager"/> implementation which is used if none are registered with the 
        /// Dependency Injection engine.
        /// </summary>
        /// <param name="instance">The <see cref="IWebHookRegistrationsManager"/> to use. If <c>null</c> then a default implementation is used.</param>
        public static void SetRegistrationsManager(IWebHookRegistrationsManager instance)
        {
            _registrationsManager = instance;
        }

        /// <summary>
        /// For testing purposes
        /// </summary>
        internal static void Reset()
        {
            _filterManager = null;
            _filterProviders = null;
            _store = null;
            _sender = null;
            _manager = null;
            _registrationsManager = null;
            _user = null;
        }
    }
}
