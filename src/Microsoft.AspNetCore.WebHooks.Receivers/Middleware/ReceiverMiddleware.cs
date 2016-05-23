using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Middleware to Handle incoming WebHooks Requests
    /// </summary>
    public class ReceiverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly ReceiverOptions _options;

        /// <summary>
        /// Builds a new Receiver Middleware
        /// </summary>
        /// <param name="next">The next <see cref="RequestDelegate"/> in the chain</param>
        /// <param name="logger">A <see cref="ILogger"/> for the Middleware</param>
        /// <param name="options">Configured <see cref="ReceiverOptions"/> from the Startup</param>
        public ReceiverMiddleware(RequestDelegate next, ILogger<ReceiverMiddleware> logger, IOptions<ReceiverOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Invokes the Middleware to handle a HttpRequest
        /// </summary>
        /// <param name="httpContext">The HttpContext for the Request</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext)
        {

            // Get all the Receivers
            IEnumerable<IWebHookReceiver> receivers = (IEnumerable<IWebHookReceiver>) httpContext.RequestServices.GetService(typeof(IEnumerable<IWebHookReceiver>));

            // Find the Matching Receiver and capture the remaining path segment
            PathString remaining = new PathString();
            IWebHookReceiver matchingReceiver = receivers.Where(r => httpContext.Request.Path.StartsWithSegments($"/{r.Name}", out remaining)).FirstOrDefault();

            if (matchingReceiver != null)
            {

                // If we found a mathing receiver, use it to build a WebHookHandlerContext
                _logger.LogInformation("WebHooks matched the '{0}' path to the '{1}' receiver by the name '{2}'.", 
                    httpContext.Request.Path, 
                    matchingReceiver.GetType().FullName,
                    matchingReceiver.Name);

                WebHookHandlerContext context = await matchingReceiver.ReceiveAsync(remaining, httpContext);

                if (context != null)
                {
                    // If the Receiver returned a Context, then find the matching handlers
                    IEnumerable<IWebHookHandler> handlers = (IEnumerable<IWebHookHandler>)httpContext.RequestServices.GetService(typeof(IEnumerable<IWebHookHandler>));

                    if (handlers != null && handlers.Count() > 0)
                    {
                        // Sort any available handlers
                        IEnumerable<IWebHookHandler> orderedHandlers = handlers.OrderBy(h => h.Order);

                        // Execute each handler in order
                        foreach (IWebHookHandler handler in orderedHandlers)
                        {
                            if (String.IsNullOrWhiteSpace(handler.Receiver) || handler.Receiver.Equals(matchingReceiver.Name, StringComparison.CurrentCultureIgnoreCase))
                            {
                                _logger.LogInformation("Executing the '{0}' Handler with the Context from the '{1}' Receiver.", handler.GetType().FullName, matchingReceiver.GetType().FullName);
                                await handler.ExecuteAsync(remaining, context);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No Handlers were found to process the context from '{0}' Receiver.", matchingReceiver.Name);
                    }
                }
            }
            else
            {
                _logger.LogDebug("No Matching Receiver was found.");
                await _next(httpContext);
            }
        }
    }

}
