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
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ReceiverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly ReceiverOptions _options;

        public ReceiverMiddleware(RequestDelegate next, ILogger<ReceiverMiddleware> logger, IOptions<ReceiverOptions> options)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            IEnumerable<IWebHookReceiver> receivers = (IEnumerable<IWebHookReceiver>) httpContext.RequestServices.GetService(typeof(IEnumerable<IWebHookReceiver>));

            PathString remaining = new PathString();
            IWebHookReceiver matchingReceiver = receivers.Where(r => httpContext.Request.Path.StartsWithSegments(r.Name, out remaining)).FirstOrDefault();

            if (matchingReceiver != null)
            {
                _logger.LogInformation("WebHooks matched the '{0}' path to the '{1}' receiver by the name '{2}'.", 
                    httpContext.Request.Path, 
                    matchingReceiver.GetType().FullName,
                    matchingReceiver.Name);

                WebHookHandlerContext context = await matchingReceiver.ReceiveAsync(remaining, httpContext);

                if (context != null)
                {
                    IEnumerable<IWebHookHandler> handlers = (IEnumerable<IWebHookHandler>)httpContext.RequestServices.GetService(typeof(IEnumerable<IWebHookHandler>));

                    if (handlers != null && handlers.Count() > 0)
                    {
                        IEnumerable<IWebHookHandler> orderedHandlers = handlers.OrderBy(h => h.Order);

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
                else
                {
                    _logger.LogError("The Receiver '{0}' did not return a WebHookContext.", matchingReceiver.Name);
                }
            }
            else
            {
                _logger.LogDebug("No Matching Receiver was found.");
                await _next(httpContext);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class ReceiverMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebHooks(this IApplicationBuilder builder)
        {
            return builder.Map("api/webhooks/incoming", config => config.UseMiddleware<ReceiverMiddleware>());
        }
    }
}
