using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an event loop which dequeues messages from a Microsoft Azure Queue and then sends the
    /// WebHook to the recipients. If the delivery success then the message is removed from the queue, otherwise it remains so that another 
    /// attempt can be made. After a given number of attempts the message is discarded without being delivered.
    /// </summary>
    public class AzureWebHookDequeueManager : IDisposable
    {
        internal const string QueueMessageKey = "MS_QueueMessage";
        internal const int MaxDequeuedMessages = 32;

        internal static readonly TimeSpan _DefaultFrequency = TimeSpan.FromMinutes(5);
        internal static readonly TimeSpan _DefaultMessageTimeout = TimeSpan.FromMinutes(2);

        internal const string WorkItemKey = "MS_WebHookWorkItem";
        private const int DefaultMaxDequeueCount = 3;

        private readonly ILogger _logger;
        internal readonly IStorageManager _storageManager;
        private readonly WebHookSender _sender;
        internal readonly HttpClient _httpClient;
        internal readonly WebHooksAzureDequeueManagerOptions _options;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings();

        private CancellationTokenSource _tokenSource;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureWebHookDequeueManager"/> using the given <paramref name="connectionString"/>
        /// to identify the Microsoft Azure Storage Queue.
        /// </summary>
        /// <param name="connectionString">The Microsoft Azure Storage Queue connection string.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance to use for logging errors and warnings.</param>
        public AzureWebHookDequeueManager(IOptions<WebHooksAzureDequeueManagerOptions> options, ILogger<AzureWebHookDequeueManager> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _options = options.Value;
            _logger = logger;

            _httpClient = new HttpClient();
            _storageManager = new StorageManager(logger);
            _sender = new QueuedSender(this, logger);
        }

        /// <summary>
        /// Intended for Testing Purposes
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="httpClient"></param>
        /// <param name="storageManager"></param>
        /// <param name="sender"></param>
        public AzureWebHookDequeueManager(IOptions<WebHooksAzureDequeueManagerOptions> options, ILogger<AzureWebHookDequeueManager> logger, HttpClient httpClient, IStorageManager storageManager, WebHookSender webHookSender)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _options = options.Value;
            _logger = logger;

            _httpClient = httpClient;
            _storageManager = storageManager;
            _sender = webHookSender;

        }


        internal WebHookSender WebHookSender
        {
            get
            {
                return _sender;
            }
        }

        /// <summary>
        /// Start the event loop of requesting messages from the queue and send them out as WebHooks.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to terminate the event loop.</param>
        /// <returns>An awaitable <see cref="Task"/> representing the event loop.</returns>
        public Task Start(CancellationToken cancellationToken)
        {
            if (_tokenSource != null)
            {
                string msg = string.Format(AzureStorageResource.DequeueManager_Started, this.GetType().Name);
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            return DequeueAndSendWebHooks(_tokenSource.Token);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dequeues available WebHooks and sends them out to each WebHook recipient.
        /// </summary>
        protected virtual async Task DequeueAndSendWebHooks(CancellationToken cancellationToken)
        {
            bool isEmpty = false;
            while (true)
            {
                try
                {
                    do
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        // Dequeue messages from Azure queue
                        CloudQueue _queue = await _storageManager.GetCloudQueueAsync(_options.ConnectionString, AzureWebHookSender.WebHookQueue);
                        IEnumerable<CloudQueueMessage> messages = await _storageManager.GetMessagesAsync(_queue, MaxDequeuedMessages, _options.MessageTimeout);

                        // Extract the work items
                        ICollection<WebHookWorkItem> workItems = messages.Select(m =>
                        {
                            WebHookWorkItem workItem = JsonConvert.DeserializeObject<WebHookWorkItem>(m.AsString, _serializerSettings);
                            workItem.Properties[QueueMessageKey] = m;
                            return workItem;
                        }).ToArray();

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        // Submit work items to be sent to WebHook receivers
                        if (workItems.Count > 0)
                        {
                            await _sender.SendWebHookWorkItemsAsync(workItems);
                        }
                        isEmpty = workItems.Count == 0;
                    }
                    while (!isEmpty);
                }
                catch (Exception ex)
                {
                    CloudQueue _queue = await _storageManager.GetCloudQueueAsync(_options.ConnectionString, AzureWebHookSender.WebHookQueue);
                    string msg = string.Format(AzureStorageResource.DequeueManager_ErrorDequeueing, _queue.Name, ex.Message);
                    _logger.LogError(msg, ex);
                }

                try
                {
                    await Task.Delay(_options.Frequency, cancellationToken);
                }
                catch (OperationCanceledException oex)
                {
                    _logger.LogError(oex.Message, oex);
                    return;
                }
            }
        }

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    if (_tokenSource != null)
                    {
                        _tokenSource.Cancel();
                        _tokenSource.Dispose();
                    }
                    if (_httpClient != null)
                    {
                        _httpClient.Dispose();
                    }
                    if (_sender != null)
                    {
                        _sender.Dispose();
                    }
                }
            }
        }
    }
}
