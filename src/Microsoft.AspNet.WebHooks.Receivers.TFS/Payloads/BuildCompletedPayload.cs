// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Payloads
{
    /// <summary>
    /// Describes the entire payload of event '<c>build.complete</c>'.
    /// </summary>
    public class BuildCompletedPayload : BasePayload<BuildCompletedResource>
    {
    }

    /// <summary>
    /// Describes the resource that associated with <see cref="BuildCompletedPayload"/>
    /// </summary>
    public class BuildCompletedResource : BaseResource
    {
        /// <summary>
        /// Gets the build URI.
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// Gets the build identifier.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets the build number.
        /// </summary>
        [JsonProperty("buildNumber")]
        public string BuildNumber { get; set; }

        /// <summary>
        /// Gets the build URL.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets the start time of the build.
        /// </summary>
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets the finish time of the build.
        /// </summary>
        [JsonProperty("finishTime")]
        public DateTime FinishTime { get; set; }

        /// <summary>
        /// Gets the reason which triggered the build.
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }

        /// <summary>
        /// Gets the outcome status of the build.
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// Gets the build drop location.
        /// </summary>
        [JsonProperty("dropLocation")]
        public string DropLocation { get; set; }

        /// <summary>
        /// Gets the build drop.
        /// </summary>
        [JsonProperty("drop")]
        public BuildCompletedDrop Drop { get; set; }

        /// <summary>
        /// Gets the build log.
        /// </summary>
        [JsonProperty("log")]
        public BuildCompletedLog Log { get; set; }

        /// <summary>
        /// Gets the source version for the build.
        /// </summary>
        [JsonProperty("sourceGetVersion")]
        public string SourceGetVersion { get; set; }

        /// <summary>
        /// Gets the user which last changed the source.
        /// </summary>
        [JsonProperty("lastChangedBy")]
        public ResourceUser LastChangedBy { get; set; }

        /// <summary>
        /// Gets value indicating whether this build retain indefinitely.
        /// </summary>
        [JsonProperty("retainIndefinitely")]
        public bool RetainIndefinitely { get; set; }

        /// <summary>
        /// Gets value indicating whether this build has diagnostics.
        /// </summary>
        [JsonProperty("hasDiagnostics")]
        public bool HasDiagnostics { get; set; }

        /// <summary>
        /// Gets the definition of the build.
        /// </summary>
        [JsonProperty("definition")]
        public BuildCompletedDefinition Definition { get; set; }

        /// <summary>
        /// Gets the build queue.
        /// </summary>
        [JsonProperty("queue")]
        public BuildCompletedQueue Queue { get; set; }

        /// <summary>
        /// Gets build requests.
        /// </summary>
        [JsonProperty("requests")]
        public BuildCompletedRequest[] Requests { get; set; }
    }

    /// <summary>
    /// Describes build drop
    /// </summary>
    public class BuildCompletedDrop
    {
        /// <summary>
        /// Gets drop location.
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// Gets drop type.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets drop location URL.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets drop location download URL.
        /// </summary>
        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }
    }

    /// <summary>
    /// Describes build log 
    /// </summary>
    public class BuildCompletedLog
    {
        /// <summary>
        /// Gets the log type.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets the log URL.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets the log download URL.
        /// </summary>
        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }
    }

    /// <summary>
    /// Describes build definition
    /// </summary>
    public class BuildCompletedDefinition
    {
        /// <summary>
        /// Gets the size of the batch.
        /// </summary>
        [JsonProperty("batchSize")]
        public int BatchSize { get; set; }

        /// <summary>
        /// Gets the trigger type.
        /// </summary>
        [JsonProperty("triggerType")]
        public string TriggerType { get; set; }

        /// <summary>
        /// Gets the trigger type.
        /// </summary>
        [JsonProperty("definitionType")]
        public string DefinitionType { get; set; }

        /// <summary>
        /// Gets the identifier of the build definition.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets the name of the build definition.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets the URL of the build definition.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Describes the queue of the build.
    /// </summary>
    public class BuildCompletedQueue
    {
        /// <summary>
        /// Gets the type of the queue.
        /// </summary>
        [JsonProperty("queueType")]
        public string QueueType { get; set; }

        /// <summary>
        /// Gets the identifier of the queue.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets the name of the queue.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets the URL of the queue.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    /// <summary>
    /// Describes the request of the build.
    /// </summary>
    public class BuildCompletedRequest
    {
        /// <summary>
        /// Gets the identifier of the request.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets the URL of the request.
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }

        /// <summary>
        /// Gets the user associated with the request.
        /// </summary>
        [JsonProperty("requestedFor")]
        public ResourceUser RequestedFor { get; set; }
    }
}
