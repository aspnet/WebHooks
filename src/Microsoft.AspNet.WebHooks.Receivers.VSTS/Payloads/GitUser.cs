﻿using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Payloads
{
    /// <summary>
    /// Information about the git user.
    /// </summary>
    public class GitUser
    {
        /// <summary>
        /// The git user Id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// The git user display name.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// The git user unique name.
        /// </summary>
        [JsonProperty("uniqueName")]
        public string UniqueName { get; set; }

        /// <summary>
        /// The git user url.
        /// </summary>
        [JsonProperty("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// The git user's image url.
        /// </summary>
        [JsonProperty("imageUrl")]
        public Uri ImageUrl { get; set; }
    }
}