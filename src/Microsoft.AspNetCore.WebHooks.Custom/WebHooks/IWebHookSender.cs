using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for sending out WebHooks as provided by <see cref="IWebHookManager"/>. Implementation
    /// can control the format of the WebHooks as well as how they are sent including retry policies and error handling.
    /// </summary>
    public interface IWebHookSender
    {
        /// <summary>
        /// Sends out the given collection of <paramref name="workItems"/> using whatever mechanism defined by the
        /// <see cref="IWebHookSender"/> implementation.
        /// </summary>
        /// <param name="workItems">The collection of <see cref="WebHookWorkItem"/> instances to process.</param>
        Task SendWebHookWorkItemsAsync(IEnumerable<WebHookWorkItem> workItems);
    }
}
