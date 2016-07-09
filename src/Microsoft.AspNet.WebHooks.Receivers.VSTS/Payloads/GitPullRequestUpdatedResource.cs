using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Describes the resource that associated with <see cref="GitPullRequestUpdatedPayload"/>
    /// </summary>
    public class GitPullRequestUpdatedResource : GitPullRequestResource
    {

        /// <summary>
        /// The date the Pull Request was closed.
        /// </summary>
        [JsonProperty("closedDate")]
        public DateTime ClosedDate { get; set; }
    }
}