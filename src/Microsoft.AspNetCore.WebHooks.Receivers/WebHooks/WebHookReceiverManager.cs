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
                nameof(IWebHookReceiver),
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
                    "No WebHook receiver has been registered with the name '{ReceiverName}'. Please use one of the registered receivers.",
                    receiverName);

                return null;
            }
            else if (matches.Count > 1)
            {
                // ??? Formatting is definitely optimized for the ConsoleLogger. What makes sense for the general case?
                var receiverTypes = string.Join(Environment.NewLine, matches.Select(p => p.GetType()));
                _logger.LogError(
                    2,
                    "Multiple types were found that match the WebHook receiver named '{ReceiverName}'. This can " +
                    "happen if multiple receivers are defined with the same name but different casing which is not " +
                    "supported. The request for '{ReceiverName}' has found the following matching receivers: {NewLine}{ReceiverTypes}.",
                    receiverName,
                    Environment.NewLine,
                    receiverTypes);

                var msg = string.Format(
                    CultureInfo.CurrentCulture,
                    ReceiverResources.Manager_MultipleAmbiguousReceiversFound,
                    receiverName,
                    Environment.NewLine,
                    receiverTypes);
                throw new InvalidOperationException(msg);
            }

            return matches.First();
        }
    }
}
