using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// The link.
    /// </summary>
    public class GitLink
    {
        /// <summary>
        /// The url.
        /// </summary>
        [JsonProperty("href")]
        public Uri Href { get; set; }
    }
}