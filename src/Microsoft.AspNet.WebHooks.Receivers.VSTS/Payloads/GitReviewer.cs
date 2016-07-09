using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Information on a reviewer of a Pull Request.  Extends <see cref="GitUser"/>
    /// </summary>
    public class GitReviewer : GitUser
    {
        /// <summary>
        /// Url of reviewer.
        /// </summary>
        [JsonProperty("reviewerUrl")]
        public Uri ReviewerUrl { get; set; }

        /// <summary>
        /// The Reviewer's Vote
        /// </summary>
        [JsonProperty("vote")]
        public int Vote { get; set; }

        /// <summary>
        /// Is Container
        /// </summary>
        [JsonProperty("isContainer")]
        public bool IsContainer { get; set; }
    }
}