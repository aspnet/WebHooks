﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IAsyncResourceFilter"/> that verifies the Slack request body. Confirms the body deserializes as
    /// an <see cref="IFormCollection"/>. Then confirms the token and event name are present in the data and that the
    /// token matches the configured secret key. Adds a <see cref="SlackConstants.SubtextRequestKeyName"/> entry to the
    /// <see cref="RouteValueDictionary"/> in most cases.
    /// </summary>
    public class SlackVerifyTokenFilter : WebHookSecurityFilter, IAsyncResourceFilter, IWebHookReceiver
    {
        private readonly ModelMetadata _formCollectionMetadata;
        private readonly IModelBinder _formModelBinder;

        /// <summary>
        /// Instantiates a new <see cref="SlackVerifyTokenFilter"/> instance.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="IConfiguration"/> used to initialize <see cref="WebHookSecurityFilter.Configuration"/>.
        /// </param>
        /// <param name="hostingEnvironment">
        /// The <see cref="IHostingEnvironment" /> used to initialize
        /// <see cref="WebHookSecurityFilter.HostingEnvironment"/>.
        /// </param>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="WebHookSecurityFilter.Logger"/>.
        /// </param>
        /// /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        public SlackVerifyTokenFilter(
            IConfiguration configuration,
            IHostingEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory,
            IModelMetadataProvider metadataProvider)
            : base(configuration, hostingEnvironment, loggerFactory)
        {
            _formCollectionMetadata = metadataProvider.GetMetadataForType(typeof(IFormCollection));
            _formModelBinder = new FormCollectionModelBinder();
        }

        /// <inheritdoc />
        public string ReceiverName => SlackConstants.ReceiverName;

        /// <inheritdoc />
        public bool IsApplicable(string receiverName)
        {
            if (receiverName == null)
            {
                throw new ArgumentNullException(nameof(receiverName));
            }

            return string.Equals(ReceiverName, receiverName, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var routeData = context.RouteData;
            if (!routeData.TryGetWebHookReceiverName(out var receiverName) || !IsApplicable(receiverName))
            {
                await next();
                return;
            }

            // 1. Confirm we were reached using HTTPS.
            var request = context.HttpContext.Request;
            var errorResult = EnsureSecureConnection(receiverName, request);
            if (errorResult != null)
            {
                context.Result = errorResult;
                return;
            }

            // 2. Get IFormCollection from the request body.
            var data = await ReadAsFormDataAsync(context);
            if (data == null)
            {
                // ReadAsFormDataAsync returns null only when other filters will log and return errors about the same
                // conditions. Let those filters run.
                await next();
                return;
            }

            // 3. Ensure that the token exists and matches the expected value.
            string token = data[SlackConstants.TokenRequestFieldName];
            if (string.IsNullOrEmpty(token))
            {
                Logger.LogError(
                    0,
                    "The HTTP request body did not contain a required '{PropertyName}' property.",
                    SlackConstants.TokenRequestFieldName);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifyToken_MissingValue,
                    SlackConstants.TokenRequestFieldName);
                context.Result = new BadRequestObjectResult(message);

                return;
            }

            var secretKey = GetSecretKey(
                ReceiverName,
                routeData,
                SlackConstants.SecretKeyMinLength,
                SlackConstants.SecretKeyMaxLength);

            if (!SecretEqual(token, secretKey))
            {
                Logger.LogError(
                    1,
                    "The '{PropertyName}' value provided in the HTTP request body did not match the expected value.",
                    SlackConstants.TokenRequestFieldName);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifyToken_BadValue,
                    SlackConstants.TokenRequestFieldName);
                context.Result = new BadRequestObjectResult(message);

                return;
            }

            // 4. Get the event name and subtext.
            string eventName = data[SlackConstants.TriggerRequestFieldName];
            if (eventName != null)
            {
                // Trigger was supplied. Remove the trigger word to get subtext.
                string text = data[SlackConstants.TextRequestFieldName];
                routeData.Values[SlackConstants.SubtextRequestKeyName] = GetSubtext(eventName, text);
            }
            else if ((eventName = data[SlackConstants.CommandRequestFieldName]) != null)
            {
                // Command was supplied. No need to set subtext.
            }
            else
            {
                // Trigger and command were omitted. Set subtext to the full text (if any).
                eventName = data[SlackConstants.TextRequestFieldName];
                routeData.Values[SlackConstants.SubtextRequestKeyName] = eventName;
            }

            if (string.IsNullOrEmpty(eventName))
            {
                Logger.LogError(
                    2,
                    "The HTTP request body did not contain a required '{PropertyName1}', '{PropertyName2}', or " +
                    "'{PropertyName3}' property.",
                    SlackConstants.TriggerRequestFieldName,
                    SlackConstants.CommandRequestFieldName,
                    SlackConstants.TextRequestFieldName);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifyToken_MissingValues,
                    SlackConstants.TriggerRequestFieldName,
                    SlackConstants.CommandRequestFieldName,
                    SlackConstants.TextRequestFieldName);
                context.Result = new BadRequestObjectResult(message);

                return;
            }

            // 5. Success. Provide event name for model binding.
            routeData.Values[WebHookConstants.EventKeyName] = eventName;

            await next();
        }

        /// <summary>
        /// The 'text' parameter provided by Slack contains both the trigger and the rest of the phrase. This
        /// isolates just the rest of the phrase making it easier to get in handlers.
        /// </summary>
        /// <param name="trigger">The word triggering this Slack WebHook</param>
        /// <param name="text">The full text containing the trigger word.</param>
        /// <returns>The subtext without the trigger word.</returns>
        protected static string GetSubtext(string trigger, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.StartsWith(trigger, StringComparison.OrdinalIgnoreCase))
            {
                return text.Substring(trigger.Length).Trim();
            }

            return text;
        }

        /// <summary>
        /// Reads the HTML form URL-encoded data request entity body.
        /// </summary>
        /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="IFormCollection"/> containing data from
        /// the HTTP request entity body.
        /// </returns>
        protected virtual async Task<IFormCollection> ReadAsFormDataAsync(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;
            if (request.Body == null ||
                !request.ContentLength.HasValue ||
                request.ContentLength.Value == 0L ||
                !HttpMethods.IsPost(request.Method) ||
                !request.HasFormContentType)
            {
                // Other filters will log and return errors about these conditions.
                return null;
            }

            var modelState = context.ModelState;
            var actionContext = new ActionContext(
                context.HttpContext,
                context.RouteData,
                context.ActionDescriptor,
                modelState);

            var valueProviderFactories = context.ValueProviderFactories;
            var valueProvider = await CompositeValueProvider.CreateAsync(actionContext, valueProviderFactories);
            var bindingContext = DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                _formCollectionMetadata,
                bindingInfo: null,
                modelName: WebHookConstants.ModelStateBodyModelName);

            // Read request body.
            try
            {
                await _formModelBinder.BindModelAsync(bindingContext);
            }
            finally
            {
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            // FormCollectionModelBinder cannot fail, even when !HasFormContentType (which isn't possible here).
            Debug.Assert(bindingContext.ModelState.IsValid);
            Debug.Assert(bindingContext.Result.IsModelSet);

            // Success
            return (IFormCollection)bindingContext.Result.Model;
        }
    }
}
