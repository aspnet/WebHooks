// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Manages registered <see cref="IWebHookReceiver"/> instances.
    /// </summary>
    public class WebHookReceiverManager : IWebHookReceiverManager
    {
        private readonly ILogger _logger;
        private readonly IDictionary<string, List<IWebHookReceiver>> _receiverLookup;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookReceiverManager"/> class with the given <paramref name="receivers"/>
        /// and <paramref name="logger"/>.
        /// </summary>
        public WebHookReceiverManager(IEnumerable<IWebHookReceiver> receivers, ILoggerFactory loggerFactory)
        {
            _receiverLookup = receivers
               .GroupBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
               .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            _logger = loggerFactory.CreateLogger<WebHookReceiverManager>();

            var receiverNames = string.Join(", ", _receiverLookup.Keys);
            _logger.LogInformation(
                0,
                "Registered '{Type}' instances with the following names: {ReceiverNames}.",
                typeof(IWebHookReceiver).Name,
                receiverNames);
        }

        /// <inheritdoc />
        public IWebHookReceiver GetReceiver(string receiverName)
        {
            if (receiverName == null)
            {
                throw new ArgumentNullException(nameof(receiverName));
            }

            if (!_receiverLookup.TryGetValue(receiverName, out var matches))
            {
                _logger.LogInformation(
                    1,
                    "{ReceiverName}.",
                    receiverName);

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Manager_UnknownReceiver, receiverName);
                return null;
            }
            else if (matches.Count > 1)
            {
                _logger.LogError(
                    2,
                    "Registered '{Type}' instances with the following names: {ReceiverNames}.",
                    typeof(IWebHookReceiver).Name,
                    receiverName);

                var providerList = string.Join(Environment.NewLine, matches.Select(p => p.GetType()));
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Manager_MultipleAmbiguousReceiversFound, receiverName, Environment.NewLine, providerList);
                throw new InvalidOperationException(msg);
            }

            return matches.First();
        }
    }
}
