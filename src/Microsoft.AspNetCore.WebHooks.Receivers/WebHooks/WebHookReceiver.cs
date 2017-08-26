// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstract <see cref="IWebHookReceiver"/> implementation which can be used to base other
    /// implementations on.
    /// </summary>
    public abstract class WebHookReceiver : WebHookSharedResultBase, IWebHookReceiver
    {
        // Application setting for disabling HTTPS check
        internal const string DisableHttpsCheckKey = "MS_WebHookDisableHttpsCheck";

        // Information about the 'code' URI parameter
        internal const int CodeMinLength = 32;
        internal const int CodeMaxLength = 128;
        internal const string CodeQueryParameter = "code";

        // Errors in ModelState will serialize similar to CreateErrorResult(..., message, ...) message.
        private static readonly string ModelStateRootKey = WebHookErrorKeys.MessageKey;

        private readonly IConfiguration _configuration;
        private readonly IReadOnlyList<IWebHookHandler> _handlers;
        private readonly IReadOnlyList<IInputFormatter> _inputFormatters;
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly Func<Stream, Encoding, TextReader> _readerFactory;
        private readonly IWebHookReceiverConfig _receiverConfig;

        // ??? Why is IHttpRequestStreamReaderFactory in an Internal namespace?
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookReceiver"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> aka application settings.</param>
        /// <param name="handlerManager">The <see cref="IWebHookHandlerManager"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="optionsAccessor">
        /// The <see cref="IOptions{MvcOptions}"/> accessor for <see cref="MvcOptions"/>.
        /// </param>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        protected WebHookReceiver(
            IConfiguration configuration,
            IWebHookHandlerManager handlerManager,
            ILoggerFactory loggerFactory,
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> optionsAccessor,
            IHttpRequestStreamReaderFactory readerFactory,
            IWebHookReceiverConfig receiverConfig)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (handlerManager == null)
            {
                throw new ArgumentNullException(nameof(handlerManager));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }
            if (readerFactory == null)
            {
                throw new ArgumentNullException(nameof(readerFactory));
            }
            if (receiverConfig == null)
            {
                throw new ArgumentNullException(nameof(receiverConfig));
            }

            _configuration = configuration;
            _handlers = handlerManager.Handlers;
            Logger = loggerFactory.CreateLogger(GetType());
            _metadataProvider = metadataProvider;
            _inputFormatters = optionsAccessor.Value.InputFormatters;
            _readerFactory = readerFactory.CreateReader;
            _receiverConfig = receiverConfig;
        }

        /// <inheritdoc />
        public abstract string Name { get; }

        protected ILogger Logger { get; }

        /// <inheritdoc />
        public abstract Task<IActionResult> ReceiveAsync(string id, HttpContext context, ModelStateDictionary modelState);

        /// <summary>
        /// Reads the JSON HTTP request entity body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        internal async Task<T> ReadAsJsonAsync<T>(HttpRequest request, ModelStateDictionary modelState)
            where T : JToken
        {
            // Check that there is a request body
            if (request.Body == null || request.ContentLength == 0)
            {
                Logger.LogInformation(500, "The WebHook request entity body cannot be empty.");
                modelState.TryAddModelError(ModelStateRootKey, ReceiverResources.Receiver_NoBody);

                return null;
            }

            // Check that the request body is JSON
            if (!request.IsJson())
            {
                Logger.LogInformation(501, "The WebHook request must contain an entity body formatted as JSON.");
                modelState.TryAddModelError(ModelStateRootKey, ReceiverResources.Receiver_NoJson);

                return null;
            }

            var formatterContext = new InputFormatterContext(
                request.HttpContext,
                ModelStateRootKey,
                modelState,
                _metadataProvider.GetMetadataForType(typeof(T)),
                _readerFactory,
                treatEmptyInputAsDefaultValue: false);

            var formatter = (IInputFormatter)null;
            for (var i = 0; i < _inputFormatters.Count; i++)
            {
                if (_inputFormatters[i].CanRead(formatterContext))
                {
                    formatter = _inputFormatters[i];
                    break;
                }
            }

            if (formatter == null)
            {
                // This is a configuration error that should never occur. JSON formatters are required.
                Logger.LogCritical(
                    502,
                    "No {FormatterType} available for '{ContentType}'.",
                    nameof(IInputFormatter),
                    request.ContentType);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    ReceiverResources.Receiver_MissingFormatter,
                    nameof(IInputFormatter),
                    request.ContentType);
                throw new InvalidOperationException(message);
            }

            try
            {
                // Read request body
                var result = await formatter.ReadAsync(formatterContext);
                if (result.IsModelSet)
                {
                    return (T)result.Model;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(503, ex, "The WebHook request contained invalid JSON.");

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadJson, ex.Message);
                modelState.TryAddModelError(ModelStateRootKey, msg);
            }

            return null;
        }

        /// <summary>
        /// Provides a time consistent comparison of two secrets in the form of two byte arrays.
        /// </summary>
        /// <param name="inputA">The first secret to compare.</param>
        /// <param name="inputB">The second secret to compare.</param>
        /// <returns>Returns <c>true</c> if the two secrets are equal, <c>false</c> otherwise.</returns>
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
        /// <returns>Returns <c>true</c> if the two secrets are equal, <c>false</c> otherwise.</returns>
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
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller.")]
        protected virtual IActionResult EnsureSecureConnection(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Check to see if we have been configured to ignore this check
            var disableHttpsCheckValue = _configuration[DisableHttpsCheckKey];
            if (bool.TryParse(disableHttpsCheckValue, out var disableHttpsCheck) && disableHttpsCheck == true)
            {
                return null;
            }

            // Require HTTP unless request is local
            if (!request.IsLocal() && !request.IsHttps)
            {
                Logger.LogError(
                    504,
                    "The WebHook receiver '{ReceiverType}' requires HTTPS in order to be secure. " +
                    "Please register a WebHook URI of type '{SchemeName}'.",
                    GetType().Name,
                    Uri.UriSchemeHttps);

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_NoHttps, GetType().Name, Uri.UriSchemeHttps);
                return CreateErrorResult(StatusCodes.Status400BadRequest, msg);
            }

            return null;
        }

        /// <summary>
        /// For WebHooks providers with insufficient security considerations, the receiver can require that the WebHook URI must
        /// be an <c>https</c> URI and contain a 'code' query parameter with a value configured for that particular <paramref name="id"/>.
        /// A sample WebHook URI is '<c>https://&lt;host&gt;/api/webhooks/incoming/&lt;receiver&gt;?code=83699ec7c1d794c0c780e49a5c72972590571fd8</c>'.
        /// The 'code' parameter must be between 32 and 128 characters long.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="id">A (potentially empty) ID of a particular configuration for this <see cref="IWebHookReceiver"/>. This
        /// allows an <see cref="IWebHookReceiver"/> to support multiple WebHooks with individual configurations.</param>
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
                    505,
                    "The WebHook verification request must contain a '{ParameterName}' query parameter.",
                    CodeQueryParameter);

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_NoCode, CodeQueryParameter);
                var noCode = CreateErrorResult(StatusCodes.Status400BadRequest, msg);
                return noCode;
            }

            var secretKey = await GetReceiverConfig(request, Name, id, CodeMinLength, CodeMaxLength);
            if (!WebHookReceiver.SecretEqual(code, secretKey))
            {
                Logger.LogError(
                    506,
                    "The '{ParameterName}' query parameter provided in the HTTP request did not match the expected value.",
                    CodeQueryParameter);

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadCode, CodeQueryParameter);
                var invalidCode = CreateErrorResult(StatusCodes.Status400BadRequest, msg);
                return invalidCode;
            }

            return null;
        }

        /// <summary>
        /// Gets the locally configured WebHook secret key used to validate any signature header provided in a WebHook request.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="name">The name of the config to obtain. Typically this the name of the receiver, e.g. <c>github</c>.</param>
        /// <param name="id">A (potentially empty) ID of a particular configuration for this <see cref="IWebHookReceiver"/>. This
        /// allows an <see cref="IWebHookReceiver"/> to support multiple WebHooks with individual configurations.</param>
        /// <param name="minLength">The minimum length of the key value.</param>
        /// <param name="maxLength">The maximum length of the key value.</param>
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
            var secret = await _receiverConfig.GetReceiverConfigAsync(name, id, minLength, maxLength);
            if (secret == null)
            {
                Logger.LogCritical(
                    507,
                    "Could not find a valid configuration for WebHook receiver '{ReceiverName}' and instance '{Id}'. " +
                    "The setting must be set to a value between {MinLength} and {MaxLength} characters long.",
                    name,
                    id,
                    minLength,
                    maxLength);

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadSecret, name, id, minLength, maxLength);
                throw new InvalidOperationException(msg);
            }

            return secret;
        }

        /// <summary>
        /// Gets the value of a given HTTP request header field. If the field is either not present or has more than one value
        /// then an error is generated.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="requestHeaderName">The name of the HTTP request header to look up.</param>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/>.</param>
        /// <returns>The signature header.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual string GetRequestHeader(
            HttpRequest request,
            string requestHeaderName,
            ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (requestHeaderName == null)
            {
                throw new ArgumentNullException(nameof(requestHeaderName));
            }
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            if (!request.Headers.TryGetValue(requestHeaderName, out var headers) || headers.Count != 1)
            {
                var headersCount = headers.Count;
                Logger.LogInformation(
                    508,
                    "Expecting exactly one '{HeaderName}' header field in the WebHook request but found {HeaderCount}. " +
                    "Please ensure that the request contains exactly one '{HeaderName}' header field.",
                    requestHeaderName,
                    headersCount);

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadHeader, requestHeaderName, headersCount);
                modelState.TryAddModelError(ModelStateRootKey, msg);

                return null;
            }

            return headers;
        }

        /// <summary>
        /// Reads the JSON HTTP request entity body as a JSON object.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual Task<JObject> ReadAsJsonAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            return ReadAsJsonAsync<JObject>(request, modelState);
        }

        /// <summary>
        /// Reads the JSON HTTP request entity body as a JSON array.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual Task<JArray> ReadAsJsonArrayAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            return ReadAsJsonAsync<JArray>(request, modelState);
        }

        /// <summary>
        /// Reads the JSON HTTP request entity body as a JSON token.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual Task<JToken> ReadAsJsonTokenAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            return ReadAsJsonAsync<JToken>(request, modelState);
        }

        /// <summary>
        /// Reads the XML HTTP request entity body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual async Task<XElement> ReadAsXmlAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            // Check that there is a request body
            if (request.Body == null || request.ContentLength == 0)
            {
                Logger.LogInformation(509, "The WebHook request entity body cannot be empty.");
                modelState.TryAddModelError(ModelStateRootKey, ReceiverResources.Receiver_NoBody);

                return null;
            }

            // Check that the request body is XML
            if (!request.IsXml())
            {
                Logger.LogInformation(510, "The WebHook request must contain an entity body formatted as XML.");
                modelState.TryAddModelError(ModelStateRootKey, ReceiverResources.Receiver_NoXml);

                return null;
            }

            var formatterContext = new InputFormatterContext(
                request.HttpContext,
                ModelStateRootKey,
                modelState,
                _metadataProvider.GetMetadataForType(typeof(XElement)),
                _readerFactory,
                treatEmptyInputAsDefaultValue: false);

            var formatter = (IInputFormatter)null;
            for (var i = 0; i < _inputFormatters.Count; i++)
            {
                if (_inputFormatters[i].CanRead(formatterContext))
                {
                    formatter = _inputFormatters[i];
                    break;
                }
            }

            if (formatter == null)
            {
                // This is a configuration error that should never occur. JSON formatters are required.
                Logger.LogCritical(
                    511,
                    "No {FormatterType} available for '{ContentType}'.",
                    nameof(IInputFormatter),
                    request.ContentType);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    ReceiverResources.Receiver_MissingFormatter,
                    nameof(IInputFormatter),
                    request.ContentType);
                throw new InvalidOperationException(message);
            }

            try
            {
                // Read request body
                var result = await formatter.ReadAsync(formatterContext);
                if (result.IsModelSet)
                {
                    return (XElement)result.Model;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    512,
                    ex,
                    "The WebHook request contained invalid XML.");

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadXml, ex.Message);
                modelState.TryAddModelError(ModelStateRootKey, msg);
            }

            return null;
        }

        /// <summary>
        /// Reads the HTML Form Data HTTP request entity body as an <see cref="IFormCollection"/>.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>An <see cref="IFormCollection"/> containing the HTTP request entity body.</returns>
        /// <param name="modelState">The <see cref="ModelStateDictionary"/>.</param>
        protected virtual async Task<IFormCollection> ReadAsFormCollectionAsync(
            HttpRequest request,
            ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            // Check that there is a request body
            if (request.Body == null || request.ContentLength == 0)
            {
                Logger.LogError(513, "The WebHook request entity body cannot be empty.");
                modelState.TryAddModelError(ModelStateRootKey, ReceiverResources.Receiver_NoBody);

                return null;
            }

            // Check that the request body is form data
            if (!request.HasFormContentType)
            {
                Logger.LogError(514, "The WebHook request must contain an entity body formatted as HTML Form Data.");
                modelState.TryAddModelError(ModelStateRootKey, ReceiverResources.Receiver_NoFormData);

                return null;
            }

            try
            {
                // Read request body
                var result = await request.ReadFormAsync();
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    515,
                    ex,
                    "The WebHook request contained invalid HTML Form Data.");

                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadFormData, ex.Message);
                modelState.TryAddModelError(ModelStateRootKey, msg);

                return null;
            }
        }

        /// <summary>
        /// Reads the HTML Form Data HTTP request entity body as a <see cref="NameValueCollection"/>.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>An <see cref="NameValueCollection"/> containing the HTTP request entity body.</returns>
        protected virtual async Task<NameValueCollection> ReadAsFormDataAsync(
            HttpRequest request,
            ModelStateDictionary modelState)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            var formCollection = await ReadAsFormCollectionAsync(request, modelState);
            var formData = new NameValueCollection(formCollection.Count);
            foreach (var entry in formCollection)
            {
                formData.Add(entry.Key, entry.Value);
            }

            return formData;
        }

        /// <summary>
        ///  Creates a 405 "Method Not Allowed" response which a receiver can use to indicate that a request with a
        ///  non-support HTTP method could not be processed.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A fully initialized "Method Not Allowed" <see cref="HttpResponse"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual IActionResult CreateBadMethodResult(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Logger.LogError(
                516,
                "The HTTP '{RequestMethod}' method is not supported by the '{ReceiverType}' WebHook receiver.",
                request.Method,
                GetType().Name);

            var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadMethod, request.Method, GetType().Name);
            var badMethod = CreateErrorResult(StatusCodes.Status405MethodNotAllowed, msg);

            return badMethod;
        }

        /// <summary>
        ///  Creates a 400 "Bad Request" response which a receiver can use to indicate that a request had an invalid signature
        ///  and as a result could not be processed.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="signatureHeaderName">The name of the HTTP header with invalid contents.</param>
        /// <returns>A fully initialized "Bad Request" <see cref="HttpResponse"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual IActionResult CreateBadSignatureResult(HttpRequest request, string signatureHeaderName)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            Logger.LogError(
                517,
                "The WebHook signature provided by the '{HeaderName}' header field does not match the value expected " +
                "by the '{ReceiverType}' receiver. WebHook request is invalid.",
                signatureHeaderName,
                GetType().Name);

            var msg = string.Format(
                CultureInfo.CurrentCulture,
                ReceiverResources.Receiver_BadSignature,
                signatureHeaderName,
                GetType().Name);
            var badSignature = CreateErrorResult(StatusCodes.Status400BadRequest, msg);

            return badSignature;
        }

        /// <summary>
        /// Processes the WebHook request by calling all registered <see cref="IWebHookHandler"/> instances.
        /// </summary>
        /// <param name="id">A (potentially empty) ID of a particular configuration for this <see cref="IWebHookReceiver"/>. This
        /// allows an <see cref="IWebHookReceiver"/> to support multiple WebHooks with individual configurations.</param>
        /// <param name="context">The <see cref="HttpContext"/> for this WebHook invocation.</param>
        /// <param name="request">The <see cref="HttpRequest"/> for this WebHook invocation.</param>
        /// <param name="actions">The collection of actions associated with this WebHook invocation.</param>
        /// <param name="data">Optional data associated with this WebHook invocation.</param>
        protected virtual async Task<IActionResult> ExecuteWebHookAsync(
            string id,
            HttpContext context,
            HttpRequest request,
            StringValues actions,
            object data)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Execute handlers. Note that we wait for them to complete before
            // we return. This means that we don't send back the final response
            // before all handlers have executed. As a result, we expect handlers
            // to be fairly quick in what they process. If a handler sets the
            // RequestHandled property on the context, then the execution is stopped
            // and that response returned. If a handler throws an exception then
            // the execution of handlers is also stopped.
            var handlerContext = new WebHookHandlerContext(actions)
            {
                HttpContext = context,
                Id = id,
                Data = data,
            };

            foreach (var handler in _handlers)
            {
                // Only call handlers with matching receiver name (or no receiver name in which case they support all receivers)
                if (handler.Receiver != null && !string.Equals(Name, handler.Receiver, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await handler.ExecuteAsync(Name, handlerContext);

                // Check if result has been set and if so stop the processing.
                if (handlerContext.Result != null)
                {
                    return handlerContext.Result;
                }
            }

            return null;
        }
    }
}
