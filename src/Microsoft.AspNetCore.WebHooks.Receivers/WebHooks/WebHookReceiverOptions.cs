using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Class to pass Options for WebHook Receiver Infrastructure
    /// </summary>
    public class WebHookReceiverOptions
    {
        /// <summary>
        /// Option to Turn off Https Security Check
        /// </summary>
        public bool DisableHttpsCheck { get; set; }
    }
}
