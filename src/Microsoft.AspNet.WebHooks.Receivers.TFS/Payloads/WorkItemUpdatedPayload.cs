// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Payloads
{
    /// <summary>
    /// Describes the entire payload of event '<c>workitem.updated</c>'.
    /// </summary>
    public class WorkItemUpdatedPayload : BasePayload<WorkItemUpdatedResource>
    {
        
    }

    /// <summary>
    /// Describes the resource that associated with <see cref="WorkItemUpdatedPayload"/>
    /// </summary>
    public class WorkItemUpdatedResource : BaseWorkItemResource<WorkItemUpdatedFields>
    {
        /// <summary>
        /// Gets WorkItem identifier.
        /// </summary>
        [JsonProperty("workItemId")]
        public int WorkItemId { get; set; }

        /// <summary>
        /// Gets the author of revision.
        /// </summary>
        [JsonProperty("revisedBy")]
        public ResourceUser RevisedBy { get; set; }

        /// <summary>
        /// Gets the revised date.
        /// </summary>
        [JsonProperty("revisedDate")]
        public DateTime RevisedDate { get; set; }

        /// <summary>
        /// Gets the revision.
        /// </summary>
        [JsonProperty("revision")]
        public WorkItemUpdatedRevision Revision { get; set; }
    }

    /// <summary>
    /// Describes fields of the WorkItem that was updated
    /// </summary>
    public class WorkItemUpdatedFields
    {
        /// <summary>
        /// Gets the change information for the field '<c>System.Rev</c>'.
        /// </summary>
        [JsonProperty("System.Rev")]
        public WorkItemUpdatedFieldValue<string> SystemRev { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>System.AuthorizedDate</c>'.
        /// </summary>
        [JsonProperty("System.AuthorizedDate")]
        public WorkItemUpdatedFieldValue<DateTime> SystemAuthorizedDate { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>System.RevisedDate</c>'.
        /// </summary>
        [JsonProperty("System.RevisedDate")]
        public WorkItemUpdatedFieldValue<DateTime> SystemRevisedDate { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>System.State</c>'.
        /// </summary>
        [JsonProperty("System.State")]
        public WorkItemUpdatedFieldValue<string> SystemState { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>System.Reason</c>'.
        /// </summary>
        [JsonProperty("System.Reason")]
        public WorkItemUpdatedFieldValue<string> SystemReason { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>System.AssignedTo</c>'.
        /// </summary>
        [JsonProperty("System.AssignedTo")]
        public WorkItemUpdatedFieldValue<string> SystemAssignedTo { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>System.ChangedDate</c>'.
        /// </summary>
        [JsonProperty("System.ChangedDate")]
        public WorkItemUpdatedFieldValue<DateTime> SystemChangedDate { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>System.Watermark</c>'.
        /// </summary>
        [JsonProperty("System.Watermark")]
        public WorkItemUpdatedFieldValue<string> SystemWatermark { get; set; }

        /// <summary>
        /// Gets the change information for the field '<c>Microsoft.VSTS.Common.Severity</c>'.
        /// </summary>
        [JsonProperty("Microsoft.VSTS.Common.Severity")]
        public WorkItemUpdatedFieldValue<string> MicrosoftVSTSCommonSeverity { get; set; }
    }

    /// <summary>
    /// Describes change of specific field
    /// </summary>
    /// <typeparam name="T">The string-type of the field that is being changed</typeparam>
    public class WorkItemUpdatedFieldValue<T>
    {
        /// <summary>
        /// Gets the value of the field before the change.
        /// </summary>
        [JsonProperty("oldValue")]
        public T OldValue { get; set; }

        /// <summary>
        /// Gets the value of the field after the change.
        /// </summary>
        [JsonProperty("newValue")]
        public T NewValue { get; set; }
    }

    /// <summary>
    /// Describes the revision
    /// </summary>
    public class WorkItemUpdatedRevision
    {
        /// <summary>
        /// Gets the identifier of the revision.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets the revision number.
        /// </summary>
        [JsonProperty("rev")]
        public int Rev { get; set; }

        /// <summary>
        /// Gets the revision fields.
        /// </summary>
        [JsonProperty("fields")]
        public WorkItemFields Fields { get; set; }

        /// <summary>
        /// Gets the revision URL.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
