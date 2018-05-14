// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebHooks.Sender;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides a base implementation of <see cref="IWebHookSender"/> that defines the default format
    /// for HTTP requests sent as WebHooks.
    /// </summary>
    public abstract class WebHookSender : IWebHookSender, IDisposable
    {
        internal const string SignatureHeaderKey = "sha256";
        internal const string SignatureHeaderValueTemplate = SignatureHeaderKey + "={0}";
        internal const string SignatureHeaderName = "ms-signature";

        private const string BodyIdKey = "Id";
        private const string BodyAttemptKey = "Attempt";
        private const string BodyPropertiesKey = "Properties";
        private const string BodyNotificationsKey = "Notifications";

        private readonly ILogger _logger;

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookSender"/> class.
        /// </summary>
        protected WebHookSender(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _logger = logger;
        }

        /// <summary>
        /// Gets the current <see cref="ILogger"/> instance.
        /// </summary>
        protected ILogger Logger
        {
            get { return _logger; }
        }

        /// <inheritdoc />
        public abstract Task SendWebHookWorkItemsAsync(IEnumerable<WebHookWorkItem> workItems);

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <b>false</b> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    // Dispose any resources
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="HttpRequestMessage"/> containing the headers and body given a <paramref name="workItem"/>.
        /// </summary>
        /// <param name="workItem">A <see cref="WebHookWorkItem"/> representing the <see cref="WebHook"/> to be sent.</param>
        /// <returns>A filled in <see cref="HttpRequestMessage"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Request is disposed by caller.")]
        protected virtual HttpRequestMessage CreateWebHookRequest(WebHookWorkItem workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            var hook = workItem.WebHook;

            // Create WebHook request
            var request = new HttpRequestMessage(HttpMethod.Post, hook.WebHookUri);

            // Fill in request body based on WebHook and work item data
            var body = CreateWebHookRequestBody(workItem);
            SignWebHookRequest(workItem, request, body);

            // Add extra request or entity headers
            foreach (var kvp in hook.Headers)
            {
                if (!request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value))
                {
                    if (!request.Content.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value))
                    {
                        var message = string.Format("Could not add header field \'{0}\' to the WebHook request for WebHook ID \'{1}\'.", kvp.Key, hook.Id);
                        _logger.LogError(message);
                    }
                }
            }

            return request;
        }

        /// <summary>
        /// Creates a <see cref="JObject"/> used as the <see cref="HttpRequestMessage"/> entity body for a <see cref="WebHook"/>.
        /// </summary>
        /// <param name="workItem">The <see cref="WebHookWorkItem"/> representing the data to be sent.</param>
        /// <returns>An initialized <see cref="JObject"/>.</returns>
        protected virtual JObject CreateWebHookRequestBody(WebHookWorkItem workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            var body = new Dictionary<string, object>
            {
                // Set properties from work item
                [BodyIdKey] = workItem.Id,
                [BodyAttemptKey] = workItem.Offset + 1
            };

            // Set properties from WebHook
            var properties = workItem.WebHook.Properties;
            if (properties != null)
            {
                body[BodyPropertiesKey] = new Dictionary<string, object>(properties);
            }

            // Set notifications
            body[BodyNotificationsKey] = workItem.Notifications;

            return JObject.FromObject(body);
        }

        /// <summary>
        /// Adds a SHA 256 signature to the <paramref name="body"/> and adds it to the <paramref name="request"/> as an
        /// HTTP header to the <see cref="HttpRequestMessage"/> along with the entity body.
        /// </summary>
        /// <param name="workItem">The current <see cref="WebHookWorkItem"/>.</param>
        /// <param name="request">The request to add the signature to.</param>
        /// <param name="body">The body to sign and add to the request.</param>
        protected virtual void SignWebHookRequest(WebHookWorkItem workItem, HttpRequestMessage request, JObject body)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }
            if (workItem.WebHook == null)
            {
                var message = string.Format("Invalid '{0}' instance: '{1}' cannot be null.", this.GetType().Name, "WebHook");
                throw new ArgumentException(message, "workItem");
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var secret = Encoding.UTF8.GetBytes(workItem.WebHook.Secret);
            using (var hasher = new HMACSHA256(secret))
            {
                var serializedBody = body.ToString();
                request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");

                var data = Encoding.UTF8.GetBytes(serializedBody);
                var sha256 = hasher.ComputeHash(data);
                var headerValue = string.Format(CultureInfo.InvariantCulture, SignatureHeaderValueTemplate, EncodingUtilities.ToHex(sha256));
                request.Headers.Add(SignatureHeaderName, headerValue);
            }
        }
    }
}
