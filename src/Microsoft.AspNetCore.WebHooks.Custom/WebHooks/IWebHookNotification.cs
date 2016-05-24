using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Standard format for a Notification
    /// </summary>
    public interface IWebHookNotification
    {
        /// <summary>
        /// The Action that is being Performed
        /// </summary>
        string Action { get; }
    }
}
