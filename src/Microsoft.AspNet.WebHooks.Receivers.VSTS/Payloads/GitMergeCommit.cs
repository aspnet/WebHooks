using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{ 
    /// <summary>
    /// Merge Commit Information
    /// </summary>
    public class GitMergeCommit
    {
        /// <summary>
        /// Commit Id
        /// </summary>
        [JsonProperty("commitId")]
        public string CommitId { get; set; }

        /// <summary>
        /// Commit Url
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }
    }
}