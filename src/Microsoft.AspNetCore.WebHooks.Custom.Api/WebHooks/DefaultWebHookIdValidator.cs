using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides a default implementation of <see cref="IWebHookIdValidator"/> which simply deletes any Id provided 
    /// by a client and instead forces a valid Id to be created on server side.
    /// </summary>
    public class DefaultWebHookIdValidator : IWebHookIdValidator
    {
        /// <inheritdoc/>
        public Task ValidateIdAsync(HttpRequest request, WebHook webHook)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            if (webHook == null)
            {
                throw new ArgumentNullException("webHook");
            }

            // Ensure we have a normalized ID for the WebHook
            webHook.Id = null;
            return Task.FromResult(true);
        }
    }
}
