// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Payloads
{
    public class WorkItemUpdatedPayload : BasePayload<WorkItemUpdatedResource>
    {
        
    }

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
        public WorkItemUpdatedFieldValue<string> SystemRev { get; set; }

        [JsonProperty("System.AuthorizedDate")]
        public WorkItemUpdatedFieldValue<DateTime> SystemAuthorizedDate { get; set; }

        [JsonProperty("System.RevisedDate")]
        public WorkItemUpdatedFieldValue<DateTime> SystemRevisedDate { get; set; }

        [JsonProperty("System.State")]
        public WorkItemUpdatedFieldValue<string> SystemState { get; set; }

        [JsonProperty("System.Reason")]
        public WorkItemUpdatedFieldValue<string> SystemReason { get; set; }

        [JsonProperty("System.AssignedTo")]
        public WorkItemUpdatedFieldValue<string> SystemAssignedTo { get; set; }

        [JsonProperty("System.ChangedDate")]
        public WorkItemUpdatedFieldValue<DateTime> SystemChangedDate { get; set; }

        [JsonProperty("System.Watermark")]
        public WorkItemUpdatedFieldValue<string> SystemWatermark { get; set; }

        [JsonProperty("Microsoft.VSTS.Common.Severity")]
        public WorkItemUpdatedFieldValue<string> MicrosoftVSTSCommonSeverity { get; set; }
    }

    public class WorkItemUpdatedFieldValue<T>
    {
        [JsonProperty("oldValue")]
        public T OldValue { get; set; }

        [JsonProperty("newValue")]
        public T NewValue { get; set; }
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
