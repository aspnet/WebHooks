// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Extension methods for <see cref="IWebHookRequestReader"/> instances.
    /// </summary>
    public static class WebHookRequestReaderExtensions
    {
        /// <summary>
        /// Read the HTTP request entity body as a <typeparamref name="TModel"/> instance.
        /// </summary>
        /// <typeparam name="TModel">The type of data to return.</typeparam>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="controllerContext">
        /// The <see cref="ControllerContext"/> for the current request and controller.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <typeparamref name="TModel"/> instance containing the
        /// HTTP request entity body.
        /// </returns>
        public static Task<TModel> ReadBodyAsync<TModel>(
            this IWebHookRequestReader requestReader,
            ControllerContext controllerContext)
        {
            if (requestReader == null)
            {
                throw new ArgumentNullException(nameof(requestReader));
            }
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            return requestReader.ReadBodyAsync<TModel>(controllerContext, controllerContext.ValueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body (formatted as HTML form URL-encoded data) as an
        /// <see cref="IFormCollection"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current request and action.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides an <see cref="IFormCollection"/> instance containing data
        /// from the HTTP request entity body.
        /// </returns>
        public static async Task<IFormCollection> ReadAsFormDataAsync(
            this IWebHookRequestReader requestReader,
            ActionContext actionContext)
        {
            if (requestReader == null)
            {
                throw new ArgumentNullException(nameof(requestReader));
            }
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            var request = actionContext.HttpContext.Request;
            if (!requestReader.IsValidPost(request) ||
                !request.HasFormContentType)
            {
                // Filters e.g. WebHookVerifyBodyTypeFilter will log and return errors about these conditions.
                return null;
            }

            // ReadFormAsync does not ensure the body can be read multiple times.
            await WebHookHttpRequestUtilities.PrepareRequestBody(request);

            // Read request body.
            IFormCollection formCollection;
            try
            {
                formCollection = await request.ReadFormAsync();
            }
            finally
            {
                request.Body.Seek(0L, SeekOrigin.Begin);
            }

            return formCollection;
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="JArray"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current request and action.</param>
        /// <param name="valueProviderFactories">
        /// The collection of configured <see cref="IValueProviderFactory"/>s.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="JArray"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes JSON input formatters have been registered.</remarks>
        public static Task<JArray> ReadAsJArrayAsync(
            this IWebHookRequestReader requestReader,
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories)
        {
            if (requestReader == null)
            {
                throw new ArgumentNullException(nameof(requestReader));
            }

            return requestReader.ReadBodyAsync<JArray>(actionContext, valueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="JArray"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="controllerContext">
        /// The <see cref="ControllerContext"/> for the current request and controller.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="JArray"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes JSON input formatters have been registered.</remarks>
        public static Task<JArray> ReadAsJArrayAsync(
            this IWebHookRequestReader requestReader,
            ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            return ReadAsJArrayAsync(requestReader, controllerContext, controllerContext.ValueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="JContainer"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current request and action.</param>
        /// <param name="valueProviderFactories">
        /// The collection of configured <see cref="IValueProviderFactory"/>s.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="JContainer"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes JSON input formatters have been registered.</remarks>
        public static Task<JContainer> ReadAsJContainerAsync(
            this IWebHookRequestReader requestReader,
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories)
        {
            if (requestReader == null)
            {
                throw new ArgumentNullException(nameof(requestReader));
            }

            return requestReader.ReadBodyAsync<JContainer>(actionContext, valueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="JContainer"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="controllerContext">
        /// The <see cref="ControllerContext"/> for the current request and controller.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="JContainer"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes JSON input formatters have been registered.</remarks>
        public static Task<JContainer> ReadAsJContainerAsync(
            this IWebHookRequestReader requestReader,
            ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            return ReadAsJContainerAsync(requestReader, controllerContext, controllerContext.ValueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="JObject"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current request and action.</param>
        /// <param name="valueProviderFactories">
        /// The collection of configured <see cref="IValueProviderFactory"/>s.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="JObject"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes JSON input formatters have been registered.</remarks>
        public static Task<JObject> ReadAsJObjectAsync(
            this IWebHookRequestReader requestReader,
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories)
        {
            if (requestReader == null)
            {
                throw new ArgumentNullException(nameof(requestReader));
            }

            return requestReader.ReadBodyAsync<JObject>(actionContext, valueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="JObject"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="controllerContext">
        /// The <see cref="ControllerContext"/> for the current request and controller.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="JObject"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes JSON input formatters have been registered.</remarks>
        public static Task<JObject> ReadAsJObjectAsync(
            this IWebHookRequestReader requestReader,
            ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            return ReadAsJObjectAsync(requestReader, controllerContext, controllerContext.ValueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="XElement"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current request and action.</param>
        /// <param name="valueProviderFactories">
        /// The collection of configured <see cref="IValueProviderFactory"/>s.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="XElement"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes XML input formatters have been registered.</remarks>
        public static Task<XElement> ReadAsXmlAsync(
            this IWebHookRequestReader requestReader,
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories)
        {
            if (requestReader == null)
            {
                throw new ArgumentNullException(nameof(requestReader));
            }

            return requestReader.ReadBodyAsync<XElement>(actionContext, valueProviderFactories);
        }

        /// <summary>
        /// Read the HTTP request entity body as a <see cref="XElement"/> instance.
        /// </summary>
        /// <param name="requestReader">The <see cref="IWebHookRequestReader"/> this method extends.</param>
        /// <param name="controllerContext">
        /// The <see cref="ControllerContext"/> for the current request and controller.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="XElement"/> instance containing the HTTP
        /// request entity body.
        /// </returns>
        /// <remarks>This method assumes XML input formatters have been registered.</remarks>
        public static Task<XElement> ReadAsXmlAsync(
            this IWebHookRequestReader requestReader,
            ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            return ReadAsXmlAsync(requestReader, controllerContext, controllerContext.ValueProviderFactories);
        }
    }
}
