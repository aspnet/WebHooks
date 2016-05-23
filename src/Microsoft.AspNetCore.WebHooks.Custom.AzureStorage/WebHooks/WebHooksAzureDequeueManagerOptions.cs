using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class WebHooksAzureDequeueManagerOptions
    {
        public string ConnectionString { get; set; }

        public TimeSpan Frequency { get; set; }

        public TimeSpan MessageTimeout { get; set; }

        public int MaxDeQueueCount { get; set; }

        internal Type Sender { get; set; }

        public WebHooksAzureDequeueManagerOptions()
        {
            Frequency = TimeSpan.FromMinutes(5);
            MessageTimeout = TimeSpan.FromMinutes(2);
            MaxDeQueueCount = 3;
            Sender = typeof(QueuedSender);
        }

        public void SetSender<T>() where T : WebHookSender
        {
            Sender = typeof(T);
        }
    }
}
