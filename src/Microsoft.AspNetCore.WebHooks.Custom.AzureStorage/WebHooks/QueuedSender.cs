using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{

    internal class QueuedSender : WebHookSender
    {
        private readonly AzureWebHookDequeueManager _parent;
        private readonly ILogger _logger;


        /// <summary>
        /// Creates a new <see cref="WebHookSender"/> for use by the <see cref="AzureWebHookDequeueManager"/>.
        /// </summary>
        /// <param name="parent">The <see cref="AzureWebHookDequeueManager"/> this Sender works for.</param>
        /// <param name="logger">A <see cref="ILogger"/> to log messages.</param>
        public QueuedSender(AzureWebHookDequeueManager parent, ILogger logger)
            : base(logger)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("parent");
            }

            _parent = parent;
            _logger = logger;

        }

        /// <inheritdoc />
        public override async Task SendWebHookWorkItemsAsync(IEnumerable<WebHookWorkItem> workItems)
        {
            if (workItems == null)
            {
                throw new ArgumentNullException("workItems");
            }

            // Keep track of which queued messages should be deleted because processing has completed.
            List<CloudQueueMessage> deleteMessages = new List<CloudQueueMessage>();

            // Submit WebHook requests in parallel
            List<Task<HttpResponseMessage>> requestTasks = new List<Task<HttpResponseMessage>>();
            foreach (var workItem in workItems)
            {
                HttpRequestMessage request = CreateWebHookRequest(workItem);
                request.Properties[AzureWebHookDequeueManager.WorkItemKey] = workItem;

                try
                {
                    Task<HttpResponseMessage> requestTask = _parent._httpClient.SendAsync(request);
                    requestTasks.Add(requestTask);
                }
                catch (Exception ex)
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.DequeueManager_SendFailure, request.RequestUri, ex.Message);
                    Logger.LogInformation(msg);

                    CloudQueueMessage message = GetMessage(workItem);
                    if (DiscardMessage(workItem, message))
                    {
                        deleteMessages.Add(message);
                    }
                }
            }

            // Wait for all responses and see which messages should be deleted from the queue based on the response statuses.
            HttpResponseMessage[] responses = await Task.WhenAll(requestTasks);
            foreach (HttpResponseMessage response in responses)
            {
                WebHookWorkItem workItem = response.RequestMessage.Properties[AzureWebHookDequeueManager.WorkItemKey] as WebHookWorkItem;
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.DequeueManager_WebHookStatus, workItem.WebHook.Id, response.StatusCode, workItem.Offset);
                Logger.LogInformation(msg);

                // If success or 'gone' HTTP status code then we remove the message from the Azure queue.
                // If error then we leave it in the queue to be consumed once it becomes visible again or we give up
                CloudQueueMessage message = GetMessage(workItem);
                if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Gone || DiscardMessage(workItem, message))
                {
                    deleteMessages.Add(message);
                }
            }

            // Delete successfully delivered messages and messages that have been attempted delivered too many times.
            CloudQueue _queue = await _parent._storageManager.GetCloudQueueAsync(_parent._options.ConnectionString, AzureWebHookSender.WebHookQueue);
            await _parent._storageManager.DeleteMessagesAsync(_queue, deleteMessages);
        }

        private CloudQueueMessage GetMessage(WebHookWorkItem workItem)
        {
            CloudQueueMessage message = workItem != null ? workItem.Properties[AzureWebHookDequeueManager.QueueMessageKey] as CloudQueueMessage : null;
            if (message == null)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.DequeueManager_NoProperty, AzureWebHookDequeueManager.QueueMessageKey, workItem.Id);
                Logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
            return message;
        }

        private bool DiscardMessage(WebHookWorkItem workItem, CloudQueueMessage message)
        {
            if (message.DequeueCount >= _parent._options.MaxDeQueueCount)
            {
                string error = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.DequeueManager_GivingUp, workItem.WebHook.Id, message.DequeueCount);
                Logger.LogError(error);
                return true;
            }
            return false;
        }
    }

}
