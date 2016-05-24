using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides context for an incoming WebHook request. The context is passed to registered <see cref="IWebHookHandler"/> implementations 
    /// which can process the incoming request accordingly.
    /// </summary>
    public class WebHookHandlerContext
    {
        private List<string> _actions;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebHookHandlerContext"/> with the given set of <paramref name="actions"/>.
        /// </summary>
        public WebHookHandlerContext(IEnumerable<string> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }
            _actions = actions.ToList();
        }

        /// <summary>
        /// Gets or sets a (potentially empty) ID of a particular configuration for this WebHook. This ID can be 
        /// used to differentiate between WebHooks from multiple senders registered with the same receiver.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Provides the set of actions that caused the WebHook to be fired.
        /// </summary>
        public ICollection<string> Actions
        {
            get
            {
                return _actions;
            }
        }

        /// <summary>
        /// Gets or sets the optional data associated with this WebHook. The data typically represents the
        /// HTTP request entity body of the incoming WebHook but can have been processed in various ways
        /// by the corresponding <see cref="IWebHookReceiver"/>.
        /// </summary>
        public object Data { get; set; }
    }
}
