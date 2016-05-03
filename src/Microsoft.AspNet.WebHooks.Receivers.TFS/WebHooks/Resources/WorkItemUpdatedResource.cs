// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public class WorkItemUpdatedResource : BaseWorkItemResource<WorkItemUpdatedFields>
    {
        [JsonProperty("workItemId")]
        public int WorkItemId { get; set; }

        [JsonProperty("revisedBy")]
        public ResourceUser RevisedBy { get; set; }

        [JsonProperty("revisedDate")]
        public DateTime RevisedDate { get; set; }

        [JsonProperty("revision")]
        public WorkItemUpdatedRevision Revision { get; set; }
    }

    public class WorkItemUpdatedFields
    {
        [JsonProperty("System.Rev")]
        public WorkItemUpdatedFieldValue SystemRev { get; set; }

        [JsonProperty("System.AuthorizedDate")]
        public WorkItemUpdatedFieldValue SystemAuthorizedDate { get; set; }

        [JsonProperty("System.RevisedDate")]
        public WorkItemUpdatedFieldValue SystemRevisedDate { get; set; }

        [JsonProperty("System.State")]
        public WorkItemUpdatedFieldValue SystemState { get; set; }

        [JsonProperty("System.Reason")]
        public WorkItemUpdatedFieldValue SystemReason { get; set; }

        [JsonProperty("System.AssignedTo")]
        public WorkItemUpdatedFieldValue SystemAssignedTo { get; set; }

        [JsonProperty("System.ChangedDate")]
        public WorkItemUpdatedFieldValue SystemChangedDate { get; set; }

        [JsonProperty("System.Watermark")]
        public WorkItemUpdatedFieldValue SystemWatermark { get; set; }

        [JsonProperty("Microsoft.VSTS.Common.Severity")]
        public WorkItemUpdatedFieldValue MicrosoftVSTSCommonSeverity { get; set; }
    }

    public class WorkItemUpdatedFieldValue
    {
        [JsonProperty("oldValue")]
        public string OldValue { get; set; }

        [JsonProperty("newValue")]
        public string NewValue { get; set; }
    }

    public class WorkItemUpdatedRevision
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rev")]
        public int Rev { get; set; }

        [JsonProperty("fields")]
        public WorkItemFields Fields { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
