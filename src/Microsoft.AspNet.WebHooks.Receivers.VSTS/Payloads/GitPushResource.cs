using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Describes the resource that associated with <see cref="GitPushPayload"/>
    /// </summary>
    public class GitPushResource : BaseResource
    {
        /// <summary>
        /// List of Commits in the push.
        /// </summary>
        [JsonProperty("commits")]
        public List<GitCommit> Commits { get; set; }

        /// <summary>
        /// List of Reference updates.
        /// </summary>
        [JsonProperty("refUpdates")]
        public List<GitRefUpdate> RefUpdates { get; set; }

        /// <summary>
        /// The repository being updated
        /// </summary>
        [JsonProperty("repository")]
        public GitRepository Repository { get; set; }

        /// <summary>
        /// The user pushing the code.
        /// </summary>
        [JsonProperty("pushedBy")]
        public GitUser PushedBy { get; set; }

        /// <summary>
        /// The Id of the push.
        /// </summary>
        [JsonProperty("pushId")]
        public int PushId { get; set; }

        /// <summary>
        /// The date of the push.
        /// </summary>
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// The Url of the push.
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// Links for the push
        /// </summary>
        [JsonProperty("_links")]
        public GitPushLinks Links { get; set; }
    }
}