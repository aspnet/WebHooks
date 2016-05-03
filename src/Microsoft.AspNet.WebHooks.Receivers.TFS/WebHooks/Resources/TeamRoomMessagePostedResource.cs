// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public class TeamRoomMessagePostedResource : BaseResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("messageType")]
        public string MessageType { get; set; }

        [JsonProperty("postedTime")]
        public DateTime PostedTime { get; set; }

        [JsonProperty("postedRoomId")]
        public int PostedRoomId { get; set; }

        [JsonProperty("postedBy")]
        public ResourceUser PostedBy { get; set; }
    }
}
