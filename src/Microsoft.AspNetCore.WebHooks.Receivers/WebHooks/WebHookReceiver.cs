// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstract <see cref="IWebHookReceiver"/> implementation which can be used to base other implementations on.
    /// </summary>
    public abstract class WebHookReceiver : WebHookSharedResultBase, IWebHookReceiver
    {
        // Application setting for disabling HTTPS check
        internal const string DisableHttpsCheckKey = "MS_WebHookDisableHttpsCheck";

        // Information about the 'code' URI parameter
        internal const int CodeMinLength = 32;
        internal const int CodeMaxLength = 128;
        internal const string CodeQueryParameter = "code";

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookReceiver"/> class.
        /// </summary>
        protected WebHookReceiver(
            IWebHookHandlerManager handlerManager,
            ILoggerFactory loggerFactory,
            IOptions<MvcOptions> optionsAccessor)
        {
            HandlerManager = handlerManager;
            InputFormatters = optionsAccessor.Value.InputFormatters;
            Logger = loggerFactory.CreateLogger(GetType());
        }

        /// <inheritdoc />
        public abstract string Name { get; }

        protected IWebHookHandlerManager HandlerManager { get; }

        protected IReadOnlyList<IWebHookHandler> Handlers => HandlerManager.Handlers;

        protected IReadOnlyList<IInputFormatter> InputFormatters { get; }

        protected ILogger Logger { get; }

        /// <inheritdoc />
        public abstract Task<IActionResult> ReceiveAsync(string id, HttpContext context, ModelStateDictionary modelState);

        /// <summary>
        /// Reads the JSON HTTP request entity body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        internal static async Task<T> ReadAsJsonAsync<T>(HttpRequest request, ModelStateDictionary modelState)
            where T : JToken
        {
            HttpConfiguration config = request.GetConfiguration();

            // Check that there is a request body
            if (request.Content == null)
            {
                var msg = ReceiverResources.Receiver_NoBody;
                config.DependencyResolver.GetLogger().Info(msg);
                HttpResponse noBody = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(noBody);
            }

            // Check that the request body is JSON
            if (!request.IsJson())
            {
                var msg = ReceiverResources.Receiver_NoJson;
                config.DependencyResolver.GetLogger().Info(msg);
                HttpResponse noJson = request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, msg);
                throw new HttpResponseException(noJson);
            }

            try
            {
                // Read request body
                T result = await request.Content.ReadAsAsync<T>(config.Formatters);
                return result;
            }
            catch (Exception ex)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadJson, ex.Message);
                config.DependencyResolver.GetLogger().Error(msg, ex);
                HttpResponse invalidBody = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg, ex);
                throw new HttpResponseException(invalidBody);
            }
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
        protected virtual void EnsureSecureConnection(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IDependencyResolver resolver = request.GetConfiguration().DependencyResolver;

            // Check to see if we have been configured to ignore this check
            SettingsDictionary settings = resolver.GetSettings();
            string disableHttpsCheckValue = settings.GetValueOrDefault(DisableHttpsCheckKey);
            if (bool.TryParse(disableHttpsCheckValue, out var disableHttpsCheck) && disableHttpsCheck == true)
            {
                return;
            }

            // Require HTTP unless request is local
            if (!request.IsLocal() && !request.RequestUri.IsHttps())
            {
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_NoHttps, GetType().Name, Uri.UriSchemeHttps);
                resolver.GetLogger().Error(msg);
                HttpResponse noHttps = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(noHttps);
            }
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
        protected virtual async Task EnsureValidCode(HttpRequest request, string id)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            EnsureSecureConnection(request);

            NameValueCollection queryParameters = request.RequestUri.ParseQueryString();
            var code = queryParameters[CodeQueryParameter];
            if (string.IsNullOrEmpty(code))
            {
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_NoCode, CodeQueryParameter);
                request.GetConfiguration().DependencyResolver.GetLogger().Error(msg);
                HttpResponse noCode = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(noCode);
            }

            var secretKey = await this.GetReceiverConfig(request, Name, id, CodeMinLength, CodeMaxLength);
            if (!WebHookReceiver.SecretEqual(code, secretKey))
            {
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadCode, CodeQueryParameter);
                request.GetConfiguration().DependencyResolver.GetLogger().Error(msg);
                HttpResponse invalidCode = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(invalidCode);
            }
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
        protected virtual async Task<string> GetReceiverConfig(HttpRequest request, string name, string id, int minLength, int maxLength)
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
            HttpConfiguration httpConfig = request.GetConfiguration();
            IWebHookReceiverConfig receiverConfig = httpConfig.DependencyResolver.GetReceiverConfig();
            var secret = await receiverConfig.GetReceiverConfigAsync(name, id, minLength, maxLength);
            if (secret == null)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadSecret, name, id, minLength, maxLength);
                httpConfig.DependencyResolver.GetLogger().Error(msg);
                HttpResponse noSecret = request.CreateErrorResponse(HttpStatusCode.InternalServerError, msg);
                throw new HttpResponseException(noSecret);
            }
            return secret;
        }

        /// <summary>
        /// Gets the value of a given HTTP request header field. If the field is either not present or has more than one value
        /// then an error is generated.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="requestHeaderName">The name of the HTTP request header to look up.</param>
        /// <returns>The signature header.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual string GetRequestHeader(HttpRequest request, string requestHeaderName)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.Headers.TryGetValues(requestHeaderName, out var headers) || headers.Count() != 1)
            {
                var headersCount = headers != null ? headers.Count() : 0;
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadHeader, requestHeaderName, headersCount);
                request.GetConfiguration().DependencyResolver.GetLogger().Info(msg);
                HttpResponse noHeader = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(noHeader);
            }

            return headers.First();
        }

        /// <summary>
        /// Reads the JSON HTTP request entity body as a JSON object.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual Task<JObject> ReadAsJsonAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            return ReadAsJsonAsync<JObject>(request);
        }

        /// <summary>
        /// Reads the JSON HTTP request entity body as a JSON array.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual Task<JArray> ReadAsJsonArrayAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            return ReadAsJsonAsync<JArray>(request);
        }

        /// <summary>
        /// Reads the JSON HTTP request entity body as a JSON token.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual Task<JToken> ReadAsJsonTokenAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            return ReadAsJsonAsync<JToken>(request);
        }

        /// <summary>
        /// Reads the XML HTTP request entity body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="JObject"/> containing the HTTP request entity body.</returns>
        protected virtual async Task<XElement> ReadAsXmlAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            HttpConfiguration config = request.GetConfiguration();

            // Check that there is a request body
            if (request.Content == null)
            {
                var msg = ReceiverResources.Receiver_NoBody;
                config.DependencyResolver.GetLogger().Info(msg);
                HttpResponse noBody = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(noBody);
            }

            // Check that the request body is XML
            if (!request.Content.IsXml())
            {
                var msg = ReceiverResources.Receiver_NoXml;
                config.DependencyResolver.GetLogger().Info(msg);
                HttpResponse noXml = request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, msg);
                throw new HttpResponseException(noXml);
            }

            try
            {
                // Read request body
                XElement result = await request.Content.ReadAsAsync<XElement>(config.Formatters);
                return result;
            }
            catch (Exception ex)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadXml, ex.Message);
                config.DependencyResolver.GetLogger().Error(msg, ex);
                HttpResponse invalidBody = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg, ex);
                throw new HttpResponseException(invalidBody);
            }
        }

        /// <summary>
        /// Reads the HTML Form Data HTTP request entity body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A <see cref="NameValueCollection"/> containing the HTTP request entity body.</returns>
        protected virtual async Task<NameValueCollection> ReadAsFormDataAsync(HttpRequest request, ModelStateDictionary modelState)
        {
            // Check that there is a request body
            if (request.Content == null)
            {
                var msg = ReceiverResources.Receiver_NoBody;
                request.GetConfiguration().DependencyResolver.GetLogger().Info(msg);
                HttpResponse noBody = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(noBody);
            }

            // Check that the request body is form data
            if (!request.Content.IsFormData())
            {
                var msg = ReceiverResources.Receiver_NoFormData;
                request.GetConfiguration().DependencyResolver.GetLogger().Info(msg);
                HttpResponse noJson = request.CreateErrorResponse(HttpStatusCode.UnsupportedMediaType, msg);
                throw new HttpResponseException(noJson);
            }

            try
            {
                // Read request body
                NameValueCollection result = await request.Content.ReadAsFormDataAsync();
                return result;
            }
            catch (Exception ex)
            {
                var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadFormData, ex.Message);
                request.GetConfiguration().DependencyResolver.GetLogger().Error(msg, ex);
                HttpResponse invalidBody = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg, ex);
                throw new HttpResponseException(invalidBody);
            }
        }

        /// <summary>
        ///  Creates a 405 "Method Not Allowed" response which a receiver can use to indicate that a request with a
        ///  non-support HTTP method could not be processed.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <returns>A fully initialized "Method Not Allowed" <see cref="HttpResponse"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        protected virtual HttpResponse CreateBadMethodResponse(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadMethod, request.Method, GetType().Name);
            request.GetConfiguration().DependencyResolver.GetLogger().Error(msg);
            HttpResponse badMethod = request.CreateErrorResponse(HttpStatusCode.MethodNotAllowed, msg);
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
        protected virtual HttpResponse CreateBadSignatureResponse(HttpRequest request, string signatureHeaderName)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var msg = string.Format(CultureInfo.CurrentCulture, ReceiverResources.Receiver_BadSignature, signatureHeaderName, GetType().Name);
            request.GetConfiguration().DependencyResolver.GetLogger().Error(msg);
            HttpResponse badSignature = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
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
        protected virtual async Task ExecuteWebHookAsync(string id, HttpContext context, HttpRequest request, IEnumerable<string> actions, object data)
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
            if (actions == null)
            {
                actions = Array.Empty<string>();
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

            foreach (var handler in Handlers)
            {
                // Only call handlers with matching receiver name (or no receiver name in which case they support all receivers)
                if (handler.Receiver != null && !string.Equals(Name, handler.Receiver, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await handler.ExecuteAsync(Name, handlerContext);

                // Check if response has been set and if so stop the processing.
                if (handlerContext.RequestHandled)
                {
                    break;
                }
            }
        }
    }
}
