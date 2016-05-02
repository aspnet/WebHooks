using Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources;
using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Events
{
    public abstract class TfsEvent<T> where T : BaseResource
    {
        public string subscriptionId { get; set; }
        public int notificationId { get; set; }
        public string id { get; set; }
        public string eventType { get; set; }
        public string publisherId { get; set; }
        public Message message { get; set; }
        public DetailedMessage detailedMessage { get; set; }
        public T resource { get; set; }
        public string resourceVersion { get; set; }
        public ResourceContainer resourceContainers { get; set; }
        public DateTime createdDate { get; set; }
    }

    public class DetailedMessage
    {
        public string text { get; set; }
        public string html { get; set; }
        public string markdown { get; set; }
    }

    public class Message
    {
        public string text { get; set; }
        public string html { get; set; }
        public string markdown { get; set; }
    }

    public class ResourceContainer
    {
        public ContainerProperty collection { get; set; }
        public ContainerProperty account { get; set; }
        public ContainerProperty project { get; set; }
    }

    public class ContainerProperty
    {
        public string id { get; set; }
    }
}
