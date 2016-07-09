﻿using Newtonsoft.Json;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Links for the Pull Request
    /// </summary>
    public class GitPullLinks
    {
        /// <summary>
        /// Pull Request Link
        /// </summary>
        [JsonProperty("self")]
        public GitLink Self { get; set; }

        /// <summary>
        /// Repository Link
        /// </summary>
        [JsonProperty("repository")]
        public GitLink Repository { get; set; }

        /// <summary>
        /// Link to Work Items
        /// </summary>
        [JsonProperty("workItems")]
        public GitLink WorkItems { get; set; }

        /// <summary>
        /// Link to the Source Branch
        /// </summary>
        [JsonProperty("sourceBranch")]
        public GitLink SourceBranch { get; set; }

        /// <summary>
        /// Link to the Target Branch
        /// </summary>
        [JsonProperty("targetBranch")]
        public GitLink TargetBranch { get; set; }

        /// <summary>
        /// Link to the Source Commit
        /// </summary>
        [JsonProperty("sourceCommit")]
        public GitLink SourceCommit { get; set; }

        /// <summary>
        /// Link to the Target Commit
        /// </summary>
        [JsonProperty("targetCommit")]
        public GitLink TargetCommit { get; set; }

        /// <summary>
        /// Link to user that created the Commit
        /// </summary>
        [JsonProperty("createdBy")]
        public GitLink CreatedBy { get; set; }
    }
}