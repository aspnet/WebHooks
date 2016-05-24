using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for processing incoming WebHooks from a particular WebHook generator, for example
    /// <c>Dropbox</c>, <c>GitHub</c>, etc.
    /// </summary>
    public interface IWebHookReceiver
    {
        /// <summary>
        /// Gets the case-insensitive name of the WebHook generator that this receiver supports, for example <c>dropbox</c> or <c>net</c>.
        ///  The name provided here will map to a URI of the form '<c>https://&lt;host&gt;/api/webhooks/incoming/&lt;name&gt;</c>'.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Processes the incoming WebHook request. A receiver must process the message and provide a context for Handlers
        /// If a Receiver processes the message and Handlers should not be run, the Receiver should set the response and return null.
        /// </summary>
        /// <param name="id">A (potentially empty) ID of a particular configuration for this <see cref="IWebHookReceiver"/>. This
        /// allows an <see cref="IWebHookReceiver"/> to support multiple WebHooks with individual configurations.</param>
        /// <param name="context">The <see cref="HttpRequestContext"/> for the incoming request.</param>
        /// <returns>A <see cref="WebHookHandlerContext"/> Which contains the data and context or null to prevent handlers from running.</returns>
        Task<WebHookHandlerContext> ReceiveAsync(PathString id, HttpContext context);
    }
}
