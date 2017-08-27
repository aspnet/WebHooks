// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;          // ??? Will we run FxCop on the AspNetCore projects?
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;       // ??? BufferingHelper is pub-Internal.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Base class for <see cref="IWebHookReceiver"/> implementations. Subclasses normally also implement
    /// <see cref="Mvc.Filters.IResourceFilter"/> or <see cref="Mvc.Filters.IAsyncResourceFilter"/>.
    /// </summary>
    public abstract class WebHookReceiver : IWebHookReceiver
    {
        // Application setting for disabling HTTPS check
        internal const string DisableHttpsCheckKey = "MS_WebHookDisableHttpsCheck";

        // Information about the 'code' URI parameter
        internal const int CodeMinLength = 32;
        internal const int CodeMaxLength = 128;
        internal const string CodeQueryParameter = "code";

        /// <summary>
        /// Instantiates a new <see cref="WebHookReceiver"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="Logger"/>.
        /// </param>
        /// <param name="receiverConfig">The <see cref="IWebHookReceiverConfig"/>.</param>
        protected WebHookReceiver(ILoggerFactory loggerFactory, IWebHookReceiverConfig receiverConfig)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            if (receiverConfig == null)
            {
                throw new ArgumentNullException(nameof(receiverConfig));
            }

            Logger = loggerFactory.CreateLogger(GetType());
            ReceiverConfig = receiverConfig;
        }

        /// <inheritdoc />
        public abstract string Name { get; }

        /// <summary>
        /// Gets the current <see cref="IConfiguration"/> for the application.
        /// </summary>
        protected IConfiguration Configuration => ReceiverConfig.Configuration;

        /// <summary>
        /// Gets an <see cref="ILogger"/> for use in this class and any subclasses.
        /// </summary>
        /// <remarks>
        /// Methods in this class use <see cref="EventId"/>s that should be distinct from (higher than) those used in
        /// subclasses.
        /// </remarks>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="IWebHookReceiverConfig"/> for WebHook receivers in this application.
        /// </summary>
        protected IWebHookReceiverConfig ReceiverConfig { get; }

        /// <inheritdoc />
        public bool IsApplicable(string receiverName)
        {
            if (receiverName == null)
            {
                throw new ArgumentNullException(nameof(receiverName));
            }

            return string.Equals(Name, receiverName, StringComparison.OrdinalIgnoreCase);
        }

        // TODO: Move some of the remaining methods into base IFilterResource implementations i.e. more classes
        // like WebHookVerifyMethodFilter. Do not need all of these in every receiver / filter.

        /// <summary>
        /// Provides a time consistent comparison of two secrets in the form of two byte arrays.
        /// </summary>
        /// <param name="inputA">The first secret to compare.</param>
        /// <param name="inputB">The second secret to compare.</param>
        /// <returns>Returns <c>true</c> if the two secrets are equal; <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        protected internal static bool SecretEqual(byte[] inputA, byte[] inputB)
        {
            if (ReferenceEquals(inputA, inputB))
            {
                return true;
            }

            if (inputA == null || inputB == null || inputA.Length != inputB.Length)
            {
                return false;
            }

            var areSame = true;
            for (var i = 0; i < inputA.Length; i++)
            {
                areSame &= inputA[i] == inputB[i];
            }

            return areSame;
        }

        /// <summary>
        /// Provides a time consistent comparison of two secrets in the form of two strings.
        /// </summary>
        /// <param name="inputA">The first secret to compare.</param>
        /// <param name="inputB">The second secret to compare.</param>
        /// <returns>Returns <c>true</c> if the two secrets are equal; <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        protected internal static bool SecretEqual(string inputA, string inputB)
        {
            if (ReferenceEquals(inputA, inputB))
            {
                return true;
            }

            if (inputA == null || inputB == null || inputA.Length != inputB.Length)
            {
                return false;
            }

            var areSame = true;
            for (var i = 0; i < inputA.Length; i++)
            {
                areSame &= inputA[i] == inputB[i];
            }

            return areSame;
        }

        /// <summary>
        /// Some WebHooks rely on HTTPS for sending WebHook requests in a secure manner. A <see cref="WebHookReceiver"/>
        /// can call this method to ensure that the incoming WebHook request is using HTTPS. If the request is not
        /// using HTTPS an error will be generated and the request will not be further processed.
        /// </summary>
        /// <remarks>This method does allow local HTTP requests using <c>localhost</c>.</remarks>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>
        /// <c>null</c> in the success case. When a check fails, an <see cref="IActionResult"/> that when executed will
        /// produce a response containing details about the problem.
        /// </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller.")]
        protected virtual IActionResult EnsureSecureConnection(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Check to see if we have been configured to ignore this check
            var disableHttpsCheckValue = Configuration[DisableHttpsCheckKey];
            if (bool.TryParse(disableHttpsCheckValue, out var disableHttpsCheck) && disableHttpsCheck == true)
            {
                return null;
            }

            // Require HTTP unless request is local
            if (!request.IsLocal() && !request.IsHttps)
            {
                Logger.LogError(
                    500,
                    "The WebHook receiver '{ReceiverType}' requires HTTPS in order to be secure. " +
                    "Please register a WebHook URI of type '{SchemeName}'.",
                    GetType().Name,
                    Uri.UriSchemeHttps);

                var msg = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Receiver_NoHttps,
                    GetType().Name,
                    Uri.UriSchemeHttps);
                var noHttps = WebHookResultUtilities.CreateErrorResult(msg);

                return noHttps;
            }

            return null;
        }

        /// <summary>
        /// For WebHooks providers with insufficient security considerations, the receiver can require that the WebHook
        /// URI must be an <c>https</c> URI and contain a 'code' query parameter with a value configured for that
        /// particular <paramref name="id"/>. A sample WebHook URI is
        /// '<c>https://&lt;host&gt;/api/webhooks/incoming/&lt;receiver&gt;?code=83699ec7c1d794c0c780e49a5c72972590571fd8</c>'.
        /// The 'code' parameter must be between 32 and 128 characters long.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="id">
        /// A (potentially empty) ID of a particular configuration for this <see cref="IWebHookReceiver"/>. This
        /// allows an <see cref="IWebHookReceiver"/> to support multiple WebHooks with individual configurations.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides <c>null</c> in the success case. When a check fails,
        /// provides an <see cref="IActionResult"/> that when executed will produce a response containing details about
        /// the problem.
        /// </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Response is disposed by Web API.")]
        protected virtual async Task<IActionResult> EnsureValidCode(HttpRequest request, string id)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var result = EnsureSecureConnection(request);
            if (result != null)
            {
                return result;
            }

            var code = request.Query[CodeQueryParameter];
            if (StringValues.IsNullOrEmpty(code))
            {
                Logger.LogError(
                    501,
                    "The WebHook verification request must contain a '{ParameterName}' query parameter.",
                    CodeQueryParameter);

                var msg = string.Format(CultureInfo.CurrentCulture, Resources.Receiver_NoCode, CodeQueryParameter);
                var noCode = WebHookResultUtilities.CreateErrorResult(msg);

                return noCode;
            }

            var secretKey = await GetReceiverConfig(request, Name, id, CodeMinLength, CodeMaxLength);
            if (!WebHookReceiver.SecretEqual(code, secretKey))
            {
                Logger.LogError(
                    502,
                    "The '{ParameterName}' query parameter provided in the HTTP request did not match the expected value.",
                    CodeQueryParameter);

                var msg = string.Format(CultureInfo.CurrentCulture, Resources.Receiver_BadCode, CodeQueryParameter);
                var invalidCode = WebHookResultUtilities.CreateErrorResult(msg);

                return invalidCode;
            }

            return null;
        }

        /// <summary>
        /// Ensure we can read the <paramref name="request"/> body without messing up JSON etc. deserialization. Body
        /// will be read at least twice in most receivers.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> to prepare.</param>
        public async Task PrepareRequestBody(HttpRequest request)
        {
            if (!request.Body.CanSeek)
            {
                BufferingHelper.EnableRewind(request);
                Debug.Assert(request.Body.CanSeek);

                await request.Body.DrainAsync(CancellationToken.None);
            }

            // Always start at the beginning.
            request.Body.Seek(0L, SeekOrigin.Begin);
        }

        /// <summary>
        /// Gets the locally configured WebHook secret key used to validate any signature header provided in a WebHook
        /// request.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="name">
        /// The name of the config to obtain. Typically this the name of the receiver, e.g. <c>github</c>.
        /// </param>
        /// <param name="id">
        /// A (potentially empty) ID of a particular configuration for this <see cref="IWebHookReceiver"/>. This
        /// allows an <see cref="IWebHookReceiver"/> to support multiple WebHooks with individual configurations.
        /// </param>
        /// <param name="minLength">The minimum length of the key value.</param>
        /// <param name="maxLength">The maximum length of the key value.</param>
        /// <returns>A <see cref="Task"/> that on completion provides the configured WebHook secret key.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual async Task<string> GetReceiverConfig(
            HttpRequest request,
            string name,
            string id,
            int minLength,
            int maxLength)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Look up configuration for this receiver and instance
            var secret = await ReceiverConfig.GetReceiverConfigAsync(name, id, minLength, maxLength);
            if (secret == null)
            {
                Logger.LogCritical(
                    503,
                    "Could not find a valid configuration for WebHook receiver '{ReceiverName}' and instance '{Id}'. " +
                    "The setting must be set to a value between {MinLength} and {MaxLength} characters long.",
                    name,
                    id,
                    minLength,
                    maxLength);

                var msg = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Receiver_BadSecret,
                    name,
                    id,
                    minLength,
                    maxLength);
                throw new InvalidOperationException(msg);
            }

            return secret;
        }

        /// <summary>
        /// Gets the value of a given HTTP request <paramref name="headerName"/>. If the field is either not present in
        /// the <paramref name="request"/> or has more than one value then an error is generated and returned in
        /// <paramref name="errorResult"/>.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="headerName">The name of the HTTP request header to look up.</param>
        /// <param name="errorResult">
        /// Set to <c>null</c> in the success case. When a check fails, an <see cref="IActionResult"/> that when
        /// executed will produce a response containing details about the problem.
        /// </param>
        /// <returns>The signature header; <c>null</c> if this cannot be found.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual string GetRequestHeader(
            HttpRequest request,
            string headerName,
            out IActionResult errorResult)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            if (!request.Headers.TryGetValue(headerName, out var headers) || headers.Count != 1)
            {
                var headersCount = headers.Count;
                Logger.LogInformation(
                    504,
                    "Expecting exactly one '{HeaderName}' header field in the WebHook request but found {HeaderCount}. " +
                    "Please ensure that the request contains exactly one '{HeaderName}' header field.",
                    headerName,
                    headersCount);

                var msg = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Receiver_BadHeader,
                    headerName,
                    headersCount);
                errorResult = WebHookResultUtilities.CreateErrorResult(msg);

                return null;
            }

            errorResult = null;

            return headers;
        }

        /// <summary>
        /// Returns a new <see cref="IActionResult"/> that when executed produces a response indicating that a
        /// request had an invalid signature and as a result could not be processed.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="signatureHeaderName">The name of the HTTP header with invalid contents.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> that when executed will produce a response with status code 400 "Bad
        /// Request" and containing details about the problem.
        /// </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual IActionResult CreateBadSignatureResult(HttpRequest request, string signatureHeaderName)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Logger.LogError(
                505,
                "The WebHook signature provided by the '{HeaderName}' header field does not match the value expected " +
                "by the '{ReceiverType}' receiver. WebHook request is invalid.",
                signatureHeaderName,
                GetType().Name);

            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Resources.Receiver_BadSignature,
                signatureHeaderName,
                GetType().Name);
            var badSignature = WebHookResultUtilities.CreateErrorResult(msg);

            return badSignature;
        }
    }
}