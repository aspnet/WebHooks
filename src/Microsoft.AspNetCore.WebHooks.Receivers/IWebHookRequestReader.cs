// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for a service that parses the request body. For use in filters that execute prior to
    /// regular model binding or in actions that read the request body after regular model binding.
    /// </summary>
    public interface IWebHookRequestReader
    {
        /// <summary>
        /// Get an indication whether the <paramref name="request"/> is suitable to be read by
        /// <see cref="ReadBodyAsync{TModel}"/> or a similar request entity body reader.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/> to check.</param>
        /// <returns>
        /// <see langword="true"/> if the <paramref name="request"/> is suitable to be read by
        /// <see cref="ReadBodyAsync{TModel}"/>; <see langword="false"/> otherwise.
        /// </returns>
        bool IsValidPost(HttpRequest request);

        /// <summary>
        /// Read the HTTP request entity body as a <typeparamref name="TModel"/> instance.
        /// </summary>
        /// <typeparam name="TModel">The type of data to return.</typeparam>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current request and action.</param>
        /// <param name="valueProviderFactories">
        /// The collection of configured <see cref="IValueProviderFactory"/>s.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <typeparamref name="TModel"/> instance containing the
        /// HTTP request entity body.
        /// </returns>
        Task<TModel> ReadBodyAsync<TModel>(
            ActionContext actionContext,
            IList<IValueProviderFactory> valueProviderFactories);
    }
}
