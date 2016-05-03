// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public class CodeCheckedInResource : BaseResource
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("buildNumber")]
        public string BuildNumber { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("finishTime")]
        public DateTime FinishTime { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("dropLocation")]
        public string DropLocation { get; set; }

        [JsonProperty("drop")]
        public CodeCheckedInDrop Drop { get; set; }

        [JsonProperty("log")]
        public CodeCheckedInLog Log { get; set; }

        [JsonProperty("sourceGetVersion")]
        public string SourceGetVersion { get; set; }

        [JsonProperty("lastChangedBy")]
        public ResourceUser LastChangedBy { get; set; }

        [JsonProperty("retainIndefinitely")]
        public bool RetainIndefinitely { get; set; }

        [JsonProperty("hasDiagnostics")]
        public bool HasDiagnostics { get; set; }

        [JsonProperty("definition")]
        public CodeCheckedInDefinition Definition { get; set; }

        [JsonProperty("queue")]
        public CodeCheckedInQueue Queue { get; set; }

        [JsonProperty("requests")]
        public CodeCheckedInRequest[] Requests { get; set; }
    }

    public class CodeCheckedInDrop
    {
        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }
    }

    public class CodeCheckedInLog
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }
    }

    public class CodeCheckedInDefinition
    {
        [JsonProperty("batchSize")]
        public int BatchSize { get; set; }

        [JsonProperty("triggerType")]
        public string TriggerType { get; set; }

        [JsonProperty("definitionType")]
        public string DefinitionType { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class CodeCheckedInQueue
    {
        [JsonProperty("queueType")]
        public string QueueType { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class CodeCheckedInRequest
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("requestedFor")]
        public ResourceUser RequestedFor { get; set; }
    }
}
