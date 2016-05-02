
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public abstract class BaseWorkItemResource<T> : BaseResource
    {
        public int id { get; set; }
        public int rev { get; set; }
        public T fields { get; set; }
        public WorkItemLinks _links { get; set; }
        public string url { get; set; }
    }

    public class WorkItemFields
    {
        public string SystemAreaPath { get; set; }
        public string SystemTeamProject { get; set; }
        public string SystemIterationPath { get; set; }
        public string SystemWorkItemType { get; set; }
        public string SystemState { get; set; }
        public string SystemReason { get; set; }
        public DateTime SystemCreatedDate { get; set; }
        public string SystemCreatedBy { get; set; }
        public DateTime SystemChangedDate { get; set; }
        public string SystemChangedBy { get; set; }
        public string SystemTitle { get; set; }
        public string MicrosoftVSTSCommonSeverity { get; set; }
        public string WEF_EB329F44FE5F4A94ACB1DA153FDF38BA_KanbanColumn { get; set; }
        public string SystemHistory { get; set; }
    }

    public class WorkItemLinks
    {
        public WorkItemLink self { get; set; }
        public WorkItemLink workItemUpdates { get; set; }
        public WorkItemLink workItemRevisions { get; set; }
        public WorkItemLink workItemType { get; set; }
        public WorkItemLink fields { get; set; }
        public WorkItemLink html { get; set; }
        public WorkItemLink workItemHistory { get; set; }
    }

    public class WorkItemLink
    {
        public string href { get; set; }
    }
}
