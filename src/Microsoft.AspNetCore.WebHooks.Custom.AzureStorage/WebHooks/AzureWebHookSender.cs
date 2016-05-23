using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an implementation of <see cref="IWebHookSender"/> sending WebHooks to a Microsoft Azure Queue for later processing.
    /// </summary>
    public class AzureWebHookSender : IWebHookSender
    {
        internal const string WebHookQueue = "webhooks";

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { Formatting = Formatting.None };
        private readonly IStorageManager _manager;
        private readonly ILogger _logger;
        private readonly WebHooksAzureStorageOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureWebHookStore"/> class with the given <paramref name="manager"/>,
        /// <paramref name="settings"/>, and <paramref name="logger"/>.
        /// </summary>
        public AzureWebHookSender(IStorageManager manager, IOptions<WebHooksAzureStorageOptions> options, ILogger logger)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _manager = manager;
            _options = options.Value;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task SendWebHookWorkItemsAsync(IEnumerable<WebHookWorkItem> workItems)
        {
            if (workItems == null)
            {
                throw new ArgumentNullException("workItems");
            }

            // Serialize WebHook requests and convert to queue messages
            IEnumerable<CloudQueueMessage> messages = null;
            try
            {
                messages = workItems.Select(item =>
                {
                    string content = JsonConvert.SerializeObject(item, _serializerSettings);
                    CloudQueueMessage message = new CloudQueueMessage(content);
                    return message;
                }).ToArray();
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.AzureSender_SerializeFailure, ex.Message);
                _logger.LogError(msg, ex);
                throw new InvalidOperationException(msg);
            }

            // Insert queue messages into queue.
            CloudQueue queue = await _manager.GetCloudQueueAsync(_options.ConnectionString, WebHookQueue);
            await _manager.AddMessagesAsync(queue, messages);
        }
    }
}
