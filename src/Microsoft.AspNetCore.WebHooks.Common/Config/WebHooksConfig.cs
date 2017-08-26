﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Web.Http;
using Microsoft.AspNetCore.WebHooks.Diagnostics;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Services;

namespace Microsoft.AspNetCore.WebHooks.Config
{
    /// <summary>
    /// Provides initialization for WebHooks.
    /// </summary>
    public static class WebHooksConfig
    {
        private static HttpConfiguration _httpConfig;

        /// <summary>
        /// Gets the <see cref="HttpConfiguration"/> that this class was initialized with.
        /// </summary>
        public static HttpConfiguration Config
        {
            get
            {
                if (_httpConfig == null)
                {
                    string initializer = typeof(WebHooksConfig).Name + ".Initialize";
                    string msg = string.Format(CultureInfo.CurrentCulture, CommonResources.Config_NotInitialized, initializer);
                    ILogger logger = CommonServices.GetLogger();
                    logger.Error(msg);
                    throw new InvalidOperationException(msg);
                }
                return _httpConfig;
            }
        }

        /// <summary>
        /// Ensures that the module is loaded on startup.
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> to use for initializing WebHooks.</param>
        public static void Initialize(HttpConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _httpConfig = config;
        }

        /// <summary>
        /// For testing purposes
        /// </summary>
        internal static void Reset()
        {
            _httpConfig = null;
        }
    }
}