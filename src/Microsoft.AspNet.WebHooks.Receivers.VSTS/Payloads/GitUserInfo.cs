using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Display information about a git user
    /// </summary>
    public class GitUserInfo
    {
        /// <summary>
        /// Name of the user
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Email of the user.
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// A date associated with the user.
        /// </summary>
        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}