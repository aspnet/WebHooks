// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Payloads
{
    public abstract class BasePayload<T> where T : BaseResource
    {
        [JsonProperty("subscriptionId")]
        public string SubscriptionId { get; set; }

        [JsonProperty("notificationId")]
        public int NotificationId { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("publisherId")]
        public string PublisherId { get; set; }

        [JsonProperty("message")]
        public PayloadMessage Message { get; set; }

        [JsonProperty("detailedMessage")]
        public PayloadMessage DetailedMessage { get; set; }

        [JsonProperty("resource")]
        public T Resource { get; set; }

        [JsonProperty("resourceVersion")]
        public string ResourceVersion { get; set; }

        [JsonProperty("resourceContainers")]
        public PayloadResourceContainers ResourceContainers { get; set; }

        [JsonProperty("createdDate")]
        public DateTime CreatedDate { get; set; }
    }

    public class PayloadMessage
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("html")]
        public string Html { get; set; }

        [JsonProperty("markdown")]
        public string Markdown { get; set; }
    }

    public class PayloadResourceContainers
    {
        [JsonProperty("collection")]
        public PayloadResourceContainer Collection { get; set; }

        [JsonProperty("account")]
        public PayloadResourceContainer Account { get; set; }

        [JsonProperty("project")]
        public PayloadResourceContainer Project { get; set; }
    }

    public class PayloadResourceContainer
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
