// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public abstract class BaseWorkItemResource<T> : BaseResource
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("rev")]
        public int Revision { get; set; }

        [JsonProperty("fields")]
        public T Fields { get; set; }

        [JsonProperty("_links")]
        public WorkItemLinks Links { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class WorkItemFields
    {
        [JsonProperty("System.AreaPath")]
        public string SystemAreaPath { get; set; }

        [JsonProperty("System.TeamProject")]
        public string SystemTeamProject { get; set; }

        [JsonProperty("System.IterationPath")]
        public string SystemIterationPath { get; set; }

        [JsonProperty("System.WorkItemType")]
        public string SystemWorkItemType { get; set; }

        [JsonProperty("System.State")]
        public string SystemState { get; set; }

        [JsonProperty("System.Reason")]
        public string SystemReason { get; set; }

        [JsonProperty("System.CreatedDate")]
        public DateTime SystemCreatedDate { get; set; }

        [JsonProperty("System.CreatedBy")]
        public string SystemCreatedBy { get; set; }

        [JsonProperty("System.ChangedDate")]
        public DateTime SystemChangedDate { get; set; }

        [JsonProperty("System.ChangedBy")]
        public string SystemChangedBy { get; set; }

        [JsonProperty("System.Title")]
        public string SystemTitle { get; set; }

        [JsonProperty("Microsoft.VSTS.Common.Severity")]
        public string MicrosoftVSTSCommonSeverity { get; set; }

        [JsonProperty("WEF_EB329F44FE5F4A94ACB1DA153FDF38BA_Kanban.Column")]
        public string KanbanColumn { get; set; }

        [JsonProperty("System.History")]
        public string SystemHistory { get; set; }
    }

    public class WorkItemLinks
    {
        [JsonProperty("self")]
        public WorkItemLink Self { get; set; }

        [JsonProperty("workItemUpdates")]
        public WorkItemLink WorkItemUpdates { get; set; }

        [JsonProperty("workItemRevisions")]
        public WorkItemLink WorkItemRevisions { get; set; }

        [JsonProperty("workItemType")]
        public WorkItemLink WorkItemType { get; set; }

        [JsonProperty("fields")]
        public WorkItemLink Fields { get; set; }

        [JsonProperty("html")]
        public WorkItemLink Html { get; set; }

        [JsonProperty("workItemHistory")]
        public WorkItemLink WorkItemHistory { get; set; }
    }

    public class WorkItemLink
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }
}
