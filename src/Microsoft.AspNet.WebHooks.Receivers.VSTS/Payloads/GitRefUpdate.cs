using Newtonsoft.Json;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Information about the references.
    /// </summary>
    public class GitRefUpdate
    {
        /// <summary>
        /// The name of the reference.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The old object Id.
        /// </summary>
        [JsonProperty("oldObjectId")]
        public string OldObjectId { get; set; }

        /// <summary>
        /// The new object Id.
        /// </summary>
        [JsonProperty("newObjectId")]
        public string NewObjectId { get; set; }
    }
}