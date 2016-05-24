using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstract <see cref="IWebHookHandler" /> implementation which can be used to enqueue 
    /// WebHooks for processing outside their immediate HTTP request/response context. This can for example 
    /// be used to process WebHooks by a separate agent or at another time. It can also be used for WebHooks 
    /// where the processing take longer than permitted by the immediate HTTP request/response context.
    /// </summary>
    public abstract class WebHookQueueHandler : WebHookHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookQueueHandler"/> class with a default <see cref="WebHookHandler.Order"/> of 50
        /// and by default accepts WebHooks from all receivers. To limit which receiver this <see cref="IWebHookHandler"/>
        /// will receive WebHook requests from, set the <see cref="WebHookHandler.Receiver"/> property to the name of that receiver.
        /// </summary>
        protected WebHookQueueHandler() : base()
        {
        }

        /// <inheritdoc />
        public override async Task ExecuteAsync(string receiver, WebHookHandlerContext context)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            WebHookQueueContext queueContext = new WebHookQueueContext(receiver, context);
            await EnqueueAsync(queueContext);
        }

        /// <summary>
        /// Enqueues an incoming WebHook for processing outside its immediate HTTP request/response context.
        /// Any exception thrown will result in an HTTP error response being returned to the party generating 
        /// the WebHook.
        /// </summary>
        /// <param name="context">The <see cref="WebHookQueueContext"/> for the WebHook to be enqueued.</param>
        protected abstract Task EnqueueAsync(WebHookQueueContext context);
    }
}
