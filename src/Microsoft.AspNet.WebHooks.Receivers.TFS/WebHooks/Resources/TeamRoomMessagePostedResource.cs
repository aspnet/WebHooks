using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public class TeamRoomMessagePostedResource : BaseResource
    {
        public int id { get; set; }
        public string content { get; set; }
        public string messageType { get; set; }
        public DateTime postedTime { get; set; }
        public int postedRoomId { get; set; }
        public ResourceUser postedBy { get; set; }
    }
}
