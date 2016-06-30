using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Repository information.
    /// </summary>
    public class GitRepository
    {
        /// <summary>
        /// The Repository Id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The Repository name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The Repository Url.
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// The project.
        /// </summary>
        [JsonProperty("project")]
        public GitProject Project { get; set; }

        /// <summary>
        /// The name of the default branch.
        /// </summary>
        [JsonProperty("defaultBranch")]
        public string DefaultBranch { get; set; }

        /// <summary>
        /// The remote Url.
        /// </summary>
        [JsonProperty("remoteUrl")]
        public Uri RemoteUrl { get; set; }
    }
}