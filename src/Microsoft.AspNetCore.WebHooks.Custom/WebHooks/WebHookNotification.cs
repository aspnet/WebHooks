using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// A basic <see cref="IWebHookNotification"/> which only includes an action property
    /// </summary>
    public class WebHookNotification : IWebHookNotification
    {

        /// <summary>
        /// Creates a basic WebHookNotification
        /// </summary>
        /// <param name="action"></param>
        public WebHookNotification(string action)
        {
            Action = action;
        }

        /// <inheritdoc />
        public string Action { get; set; }
    }
}
