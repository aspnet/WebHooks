using System;

namespace Microsoft.AspNet.WebHooks.Receivers.TFS.WebHooks.Resources
{
    public class CodeCheckedInResource : BaseResource
    {
        public string uri { get; set; }
        public int id { get; set; }
        public string buildNumber { get; set; }
        public string url { get; set; }
        public DateTime startTime { get; set; }
        public DateTime finishTime { get; set; }
        public string reason { get; set; }
        public string status { get; set; }
        public string dropLocation { get; set; }
        public Drop drop { get; set; }
        public Log log { get; set; }
        public string sourceGetVersion { get; set; }
        public ResourceUser lastChangedBy { get; set; }
        public bool retainIndefinitely { get; set; }
        public bool hasDiagnostics { get; set; }
        public Definition definition { get; set; }
        public Queue queue { get; set; }
        public Request[] requests { get; set; }
    }

    public class Drop
    {
        public string location { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string downloadUrl { get; set; }
    }

    public class Log
    {
        public string type { get; set; }
        public string url { get; set; }
        public string downloadUrl { get; set; }
    }

    public class Definition
    {
        public int batchSize { get; set; }
        public string triggerType { get; set; }
        public string definitionType { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Queue
    {
        public string queueType { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Request
    {
        public int id { get; set; }
        public string url { get; set; }
        public ResourceUser requestedFor { get; set; }
    }
}
