using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstract <see cref="IWebHookHandler" /> implementation which can be used to base other implementations on.
    /// </summary>
    public abstract class WebHookHandler : IWebHookHandler
    {
        internal const int DefaultOrder = 50;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookHandler"/> class with a default <see cref="Order"/> of 50
        /// and by default accepts WebHooks from all receivers. To limit which receiver this <see cref="IWebHookHandler"/>
        /// will receive WebHook requests from, set the <see cref="Receiver"/> property to the name of that receiver.
        /// </summary>
        protected WebHookHandler()
        {
            Order = DefaultOrder;
        }

        /// <inheritdoc />
        public int Order { get; set; }

        /// <inheritdoc />
        public string Receiver { get; protected set; }

        /// <inheritdoc />
        public abstract Task ExecuteAsync(string receiver, WebHookHandlerContext context);
    }
}
