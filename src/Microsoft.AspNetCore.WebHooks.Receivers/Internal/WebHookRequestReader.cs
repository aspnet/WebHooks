// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.WebHooks.Internal
{
    /// <summary>
    /// The default <see cref="IWebHookRequestReader"/> implementation.
    /// </summary>
    public class WebHookRequestReader : IWebHookRequestReader
    {
        private readonly IModelBinder _bodyModelBinder;
        private readonly IModelMetadataProvider _metadataProvider;

        /// <summary>
        /// Instantiates a new <see cref="WebHookRequestReader"/> instance.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="optionsAccessor">
        /// The <see cref="IOptions{MvcOptions}"/> accessor for <see cref="MvcOptions"/>.
        /// </param>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        public WebHookRequestReader(
            ILoggerFactory loggerFactory,
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> optionsAccessor,
            IHttpRequestStreamReaderFactory readerFactory)
        {
            // Do not store options.ValueProviderFactories because that is only the initial value of (for example)
            // ResourceExecutingContext.ValueProviderFactories.
            var options = optionsAccessor.Value;
            _bodyModelBinder = new BodyModelBinder(options.InputFormatters, readerFactory, loggerFactory, options);
            _metadataProvider = metadataProvider;
        }

        /// <inheritdoc />
        public bool IsValidPost(HttpRequest request)
        {
            return request.Body != null &&
                request.ContentLength.HasValue &&
                request.ContentLength.Value > 0L &&
                HttpMethods.IsPost(request.Method);
        }

        /// <inheritdoc />
        /// <remarks>This method assumes the necessary input formatters have been registered.</remarks>
        public async Task<TModel> ReadBodyAsync<TModel>(
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }
            if (valueProviderFactories == null)
            {
                throw new ArgumentNullException(nameof(valueProviderFactories));
            }

            var request = actionContext.HttpContext.Request;
            if (!IsValidPost(request))
            {
                // Filters e.g. WebHookVerifyBodyTypeFilter will log and return errors about these conditions.
                return default;
            }

            var modelMetadata = _metadataProvider.GetMetadataForType(typeof(TModel));
            var bindingContext = await CreateBindingContextAsync(actionContext, valueProviderFactories, modelMetadata);

            // Read request body.
            try
            {
                await _bodyModelBinder.BindModelAsync(bindingContext);
            }
            finally
            {
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            if (!bindingContext.ModelState.IsValid)
            {
                return default;
            }

            if (!bindingContext.Result.IsModelSet)
            {
                throw new InvalidOperationException(Resources.RequestReader_ModelBindingFailed);
            }

            // Success
            return (TModel)bindingContext.Result.Model;
        }

        private static async Task<ModelBindingContext> CreateBindingContextAsync(
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories,
            ModelMetadata modelMetadata)
        {
            var valueProvider = await CompositeValueProvider.CreateAsync(actionContext, valueProviderFactories);

            return DefaultModelBindingContext.CreateBindingContext(
                actionContext,
                valueProvider,
                modelMetadata,
                bindingInfo: null,
                modelName: WebHookConstants.ModelStateBodyModelName);
        }
    }
}
