using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Information about a project.
    /// </summary>
    public class GitProject
    {
        /// <summary>
        /// The project Id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The project name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The url of the project.
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// The state of the project.
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }
    }
}