// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Payloads
{
    /// <summary>
    /// Base class for resource object which describes WorkItem event types.
    /// </summary>
    /// <typeparam name="T">Type which describes fields associated with this kind of WorkItem change</typeparam>
    public abstract class BaseWorkItemResource<T> : BaseResource
    {
        /// <summary>
        /// Gets the identifier of WorkItem.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets the revision number.
        /// </summary>
        [JsonProperty("rev")]
        public int RevisionNumber { get; set; }

        /// <summary>
        /// Gets fields associated with the WorkItem.
        /// </summary>
        [JsonProperty("fields")]
        public T Fields { get; set; }

        /// <summary>
        /// Gets links associated with the WorkItem.
        /// </summary>
        [JsonProperty("_links")]
        public WorkItemLinks Links { get; set; }

        /// <summary>
        /// Gets the URL of the WorkItem.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Describes fields of the WorkItem
    /// </summary>
    public class WorkItemFields
    {
        /// <summary>
        /// Gets the value of field <c>System.AreaPath</c>.
        /// </summary>
        [JsonProperty("System.AreaPath")]
        public string SystemAreaPath { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.TeamProject</c>.
        /// </summary>
        [JsonProperty("System.TeamProject")]
        public string SystemTeamProject { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.IterationPath</c>.
        /// </summary>
        [JsonProperty("System.IterationPath")]
        public string SystemIterationPath { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.WorkItemType</c>.
        /// </summary>
        [JsonProperty("System.WorkItemType")]
        public string SystemWorkItemType { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.State</c>.
        /// </summary>
        [JsonProperty("System.State")]
        public string SystemState { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.Reason</c>.
        /// </summary>
        [JsonProperty("System.Reason")]
        public string SystemReason { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.CreatedDate</c>.
        /// </summary>
        [JsonProperty("System.CreatedDate")]
        public DateTime SystemCreatedDate { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.CreatedBy</c>.
        /// </summary>
        [JsonProperty("System.CreatedBy")]
        public string SystemCreatedBy { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.ChangedDate</c>.
        /// </summary>
        [JsonProperty("System.ChangedDate")]
        public DateTime SystemChangedDate { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.ChangedBy</c>.
        /// </summary>
        [JsonProperty("System.ChangedBy")]
        public string SystemChangedBy { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.Title</c>.
        /// </summary>
        [JsonProperty("System.Title")]
        public string SystemTitle { get; set; }

        /// <summary>
        /// Gets the value of field <c>Microsoft.VSTS.Common.Severity</c>.
        /// </summary>
        [JsonProperty("Microsoft.VSTS.Common.Severity")]
        public string MicrosoftVSTSCommonSeverity { get; set; }

        /// <summary>
        /// Gets the value of field <c>WEF_EB329F44FE5F4A94ACB1DA153FDF38BA_Kanban.Column</c>.
        /// </summary>
        [JsonProperty("WEF_EB329F44FE5F4A94ACB1DA153FDF38BA_Kanban.Column")]
        public string KanbanColumn { get; set; }

        /// <summary>
        /// Gets the value of field <c>System.History</c>.
        /// </summary>
        [JsonProperty("System.History")]
        public string SystemHistory { get; set; }
    }

    /// <summary>
    /// Describes links of the WorkItem.
    /// </summary>
    public class WorkItemLinks
    {
        /// <summary>
        /// Gets the link to the WorkItem itself.
        /// </summary>
        [JsonProperty("self")]
        public WorkItemLink Self { get; set; }

        /// <summary>
        /// Gets the link to the parent WorkItem if exists.
        /// </summary>
        [JsonProperty("parent")]
        public WorkItemLink Parent { get; set; }

        /// <summary>
        /// Gets the link to the WorkItem' updates.
        /// </summary>
        [JsonProperty("workItemUpdates")]
        public WorkItemLink WorkItemUpdates { get; set; }

        /// <summary>
        /// Gets the link to the WorkItem's revisions.
        /// </summary>
        [JsonProperty("workItemRevisions")]
        public WorkItemLink WorkItemRevisions { get; set; }

        /// <summary>
        /// Gets the link to the WorkItem's type.
        /// </summary>
        [JsonProperty("workItemType")]
        public WorkItemLink WorkItemType { get; set; }

        /// <summary>
        /// Gets the link to the WorkItem's fields.
        /// </summary>
        [JsonProperty("fields")]
        public WorkItemLink Fields { get; set; }

        /// <summary>
        /// Gets the link to the WorkItem's HTML.
        /// </summary>
        [JsonProperty("html")]
        public WorkItemLink Html { get; set; }

        /// <summary>
        /// Gets the link to the WorkItem's history.
        /// </summary>
        [JsonProperty("workItemHistory")]
        public WorkItemLink WorkItemHistory { get; set; }
    }

    /// <summary>
    /// Describes the WorkItem's link.
    /// </summary>
    public class WorkItemLink
    {
        /// <summary>
        /// Gets the URL of WorkItem's link.
        /// </summary>
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}
