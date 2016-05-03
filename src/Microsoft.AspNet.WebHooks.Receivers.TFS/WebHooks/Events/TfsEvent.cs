// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources;
using Newtonsoft.Json;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Events
{
    public abstract class TfsEvent<T> where T : BaseResource
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
        public TfsEventMessage Message { get; set; }

        [JsonProperty("detailedMessage")]
        public TfsEventMessage DetailedMessage { get; set; }

        [JsonProperty("resource")]
        public T Resource { get; set; }

        [JsonProperty("resourceVersion")]
        public string ResourceVersion { get; set; }

        [JsonProperty("resourceContainers")]
        public TfsEventResourceContainer ResourceContainers { get; set; }

        [JsonProperty("createdDate")]
        public DateTime CreatedDate { get; set; }
    }

    public class TfsEventMessage
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("html")]
        public string Html { get; set; }

        [JsonProperty("markdown")]
        public string Markdown { get; set; }
    }

    public class TfsEventResourceContainer
    {
        [JsonProperty("collection")]
        public TfsEventContainerProperty Collection { get; set; }

        [JsonProperty("account")]
        public TfsEventContainerProperty Account { get; set; }

        [JsonProperty("project")]
        public TfsEventContainerProperty Project { get; set; }
    }

    public class TfsEventContainerProperty
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
