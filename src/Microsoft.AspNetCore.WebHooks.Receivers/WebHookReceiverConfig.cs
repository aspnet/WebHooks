// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides a default <see cref="IWebHookReceiverConfig"/> implementation which manages
    /// <see cref="IWebHookReceiver"/> configuration using application settings. The name of the application setting is
    /// '<c>MS_WebHookReceiverSecret_&lt;name&gt;</c>' where '<c>name</c>' is the name of the receiver, for example
    /// <c>github</c>. The value is a comma-separated list of secrets, using an ID to differentiate between them. For
    /// example, '<c>secret0, id1=secret1, id2=secret2</c>'. The corresponding WebHook URI is of the form
    /// '<c>https://&lt;host&gt;/api/webhooks/incoming/custom/{id}</c>'.
    /// </summary>
    public class WebHookReceiverConfig : IWebHookReceiverConfig
    {
        internal const string ConfigKeyPrefix = "MS_WebHookReceiverSecret_";
        private readonly IDictionary<string, string> _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookReceiverConfig"/> which will use the application
        /// settings set in the given <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="IConfiguration"/> to read for <see cref="IWebHookReceiver"/> configuration.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebHookReceiverConfig(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            var logger = loggerFactory.CreateLogger<WebHookReceiverConfig>();
            _settings = ReadSettings(configuration, logger);
        }

        /// <inheritdoc />
        public IConfiguration Configuration { get; }

        /// <inheritdoc />
        public virtual Task<string> GetReceiverConfigAsync(string configurationName, string id)
        {
            if (configurationName == null)
            {
                throw new ArgumentNullException(nameof(configurationName));
            }
            if (id == null)
            {
                id = string.Empty;
            }

            var key = GetConfigKey(configurationName, id);
            var result = _settings.TryGetValue(key, out var value) ? value : null;

            return Task.FromResult(result);
        }

        internal static IDictionary<string, string> ReadSettings(IConfiguration configuration, ILogger logger)
        {
            // All keys are lowercased before additions or lookups.
            var settings = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var setting in configuration.AsEnumerable())
            {
                var key = setting.Key;
                if (key.Length > ConfigKeyPrefix.Length &&
                    key.StartsWith(ConfigKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Extract configuration (again, likely receiver) name
                    var configurationName = key.Substring(ConfigKeyPrefix.Length);

                    // Parse values
                    var segments = setting.Value.SplitAndTrim(',');
                    foreach (var segment in segments)
                    {
                        var values = segment.SplitAndTrim('=');
                        if (values.Length == 1)
                        {
                            AddKey(settings, logger, configurationName, string.Empty, values[0]);
                        }
                        else if (values.Length == 2)
                        {
                            AddKey(settings, logger, configurationName, values[0], values[1]);
                        }
                        else
                        {
                            logger.LogError(
                                0,
                                "The '{Key}' application setting must have a comma-separated value of one or more secrets of the form <secret> or <id>=<secret>.",
                                key);

                            var message = string.Format(CultureInfo.CurrentCulture, Resources.Config_BadValue, key);
                            throw new InvalidOperationException(message);
                        }
                    }
                }
            }

            if (settings.Count == 0)
            {
                var format = ConfigKeyPrefix + "<receiver>";
                logger.LogError(
                    1,
                    "Did not find any applications settings of the form '{Format}'. To receive WebHooks, please add corresponding applications settings.",
                    format);
            }

            return settings;
        }

        internal static void AddKey(
            IDictionary<string, string> settings,
            ILogger logger,
            string configurationName,
            string id,
            string value)
        {
            var lookupKey = GetConfigKey(configurationName, id);

            try
            {
                settings.Add(lookupKey, value);
                logger.LogInformation(
                    2,
                    "Registered configuration setting '{ConfigurationName}' for ID '{Id}'.",
                    configurationName,
                    id);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    3,
                    ex,
                    "Could not add configuration for receiver '{ConfigurationName}' and id '{Id}'.",
                    configurationName,
                    id);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Config_AddFailure,
                    configurationName,
                    id,
                    ex.Message);
                throw new InvalidOperationException(message);
            }
        }

        internal static string GetConfigKey(string configurationName, string id)
        {
            return (configurationName + "/" + id).ToLowerInvariant();
        }
    }
}