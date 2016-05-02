using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public class WorkItemUpdatedResource : BaseWorkItemResource<WorkItemUpdatedFields>
    {
        public int workItemId { get; set; }
        public ResourceUser revisedBy { get; set; }
        public DateTime revisedDate { get; set; }
        public Revision revision { get; set; }
    }

    public class WorkItemUpdatedFields
    {
        public FieldValueChange SystemRev { get; set; }
        public FieldValueChange SystemAuthorizedDate { get; set; }
        public FieldValueChange SystemRevisedDate { get; set; }
        public FieldValueChange SystemState { get; set; }
        public FieldValueChange SystemReason { get; set; }
        public FieldValueChange SystemAssignedTo { get; set; }
        public FieldValueChange SystemChangedDate { get; set; }
        public FieldValueChange SystemWatermark { get; set; }
        public FieldValueChange MicrosoftVSTSCommonSeverity { get; set; }
    }

    public class FieldValueChange
    {
        public string oldValue { get; set; }
        public string newValue { get; set; }
    }

    public class Revision
    {
        public int id { get; set; }
        public int rev { get; set; }
        public WorkItemFields fields { get; set; }
        public string url { get; set; }
    }
}
