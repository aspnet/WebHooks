using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public class BuildCompletedResource : BaseResource
    {
        public int changesetId { get; set; }
        public string url { get; set; }
        public ResourceUser author { get; set; }
        public ResourceUser checkedInBy { get; set; }
        public DateTime createdDate { get; set; }
        public string comment { get; set; }
    }
}
