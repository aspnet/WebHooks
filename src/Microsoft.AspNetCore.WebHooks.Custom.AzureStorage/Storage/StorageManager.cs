using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Globalization;
using Microsoft.AspNetCore.WebHooks.Properties;
using System.Threading;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides utilities for managing connection strings and related information for Microsoft Azure Table Storage.
    /// </summary>

    public class StorageManager : IStorageManager
    {
        private const string AzureStoreConnectionStringName = "MS_AzureStoreConnectionString";
        private const string PartitionKey = "PartitionKey";
        private const string RowKey = "RowKey";

        private const string QuerySeparator = "&";
        private const int MaxBatchSize = 100;

        private static readonly ConcurrentDictionary<string, CloudStorageAccount> TableAccounts = new ConcurrentDictionary<string, CloudStorageAccount>();
        private static readonly ConcurrentDictionary<string, CloudStorageAccount> QueueAccounts = new ConcurrentDictionary<string, CloudStorageAccount>();

        private static IStorageManager _storageManager;

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageManager"/> class with the given <paramref name="logger"/>.
        /// </summary>
        public StorageManager(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            _logger = logger;
        }

        /// <inheritdoc />
        public CloudStorageAccount GetCloudStorageAccount(string connectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(connectionString);
                if (storageAccount == null)
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_NoCloudStorageAccount, typeof(CloudStorageAccount).Name);
                    throw new ArgumentException(msg);
                }
            }
            catch (Exception ex)
            {
                string msg = AzureStorageResource.StorageManager_InvalidConnectionString;
                _logger.LogError(msg, ex);
                throw new InvalidOperationException(msg, ex);
            }
            return storageAccount;
        }

        /// <inheritdoc />
        public async Task<CloudTable> GetCloudTableAsync(string connectionString, string tableName)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }
            if (tableName == null)
            {
                throw new ArgumentNullException("tableName");
            }

            string tableKey = GetLookupKey(connectionString, tableName);

            CloudStorageAccount storageAccount;
            if (!TableAccounts.TryGetValue(tableKey, out storageAccount))
            {
                storageAccount = GetCloudStorageAccount(connectionString);
                if (TableAccounts.TryAdd(tableKey, storageAccount))
                {
                    try
                    {
                        // Ensure that table exists
                        CloudTableClient client = storageAccount.CreateCloudTableClient();
                        CloudTable cloudTable = client.GetTableReference(tableName);
                        await cloudTable.CreateIfNotExistsAsync();
                    }
                    catch (Exception ex)
                    {
                        string error = GetStorageErrorMessage(ex);
                        string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_InitializationFailure, error);
                        _logger.LogError(msg, ex);
                        throw new InvalidOperationException(msg, ex);
                    }
                }
            }

            CloudTableClient cloudClient = storageAccount.CreateCloudTableClient();
            return cloudClient.GetTableReference(tableName);
        }

        /// <inheritdoc />
        public async Task<CloudQueue> GetCloudQueueAsync(string connectionString, string queueName)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }
            if (queueName == null)
            {
                throw new ArgumentNullException("queueName");
            }

            string queueKey = GetLookupKey(connectionString, queueName);

            CloudStorageAccount storageAccount;
            if (!QueueAccounts.TryGetValue(queueKey, out storageAccount))
            {
                storageAccount = GetCloudStorageAccount(connectionString);
                if (QueueAccounts.TryAdd(queueKey, storageAccount))
                {
                    try
                    {
                        // Ensure that queue exists
                        CloudQueueClient client = storageAccount.CreateCloudQueueClient();
                        CloudQueue cloudQueue = client.GetQueueReference(queueName);
                        await cloudQueue.CreateIfNotExistsAsync();
                    }
                    catch (Exception ex)
                    {
                        string error = GetStorageErrorMessage(ex);
                        string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_InitializationFailure, error);
                        _logger.LogError(msg, ex);
                        throw new InvalidOperationException(msg, ex);
                    }
                }
            }

            CloudQueueClient cloudClient = storageAccount.CreateCloudQueueClient();
            return cloudClient.GetQueueReference(queueName);
        }

        /// <inheritdoc />
        public void AddPartitionKeyConstraint(TableQuery query, string partitionKey)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            if (partitionKey == null)
            {
                throw new ArgumentNullException("partitionKey");
            }

            string partitionKeyFilter = string.Format(CultureInfo.InvariantCulture, "{0} eq '{1}'", PartitionKey, partitionKey);
            AddQueryConstraint(query, partitionKeyFilter);
        }

        /// <inheritdoc />
        public async Task<TableResult> ExecuteRetrievalAsync(CloudTable table, string partitionKey, string rowKey)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }
            if (partitionKey == null)
            {
                throw new ArgumentNullException("partitionKey");
            }
            if (rowKey == null)
            {
                throw new ArgumentNullException("rowKey");
            }

            try
            {
                TableOperation operation = TableOperation.Retrieve(partitionKey, rowKey);
                TableResult result = await table.ExecuteAsync(operation);
                return result;
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_ErrorRetrieving, ex.Message);
                _logger.LogError(msg, ex);
            }
            return null;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DynamicTableEntity>> ExecuteQueryAsync(CloudTable table, TableQuery query)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            try
            {
                List<DynamicTableEntity> result = new List<DynamicTableEntity>();
                TableQuerySegment segment;
                TableContinuationToken continuationToken = null;
                do
                {
                    segment = await table.ExecuteQuerySegmentedAsync(query, continuationToken);
                    if (segment == null)
                    {
                        break;
                    }
                    result.AddRange(segment);
                    continuationToken = segment.ContinuationToken;
                }
                while (continuationToken != null);
                return segment;
            }
            catch (Exception ex)
            {
                string errorMessage = GetStorageErrorMessage(ex);
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_QueryFailed, errorMessage);
                _logger.LogError(msg, ex);
                throw new InvalidOperationException(msg, ex);
            }
        }

        /// <inheritdoc />
        public async Task<TableResult> ExecuteAsync(CloudTable table, TableOperation operation)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }
            if (operation == null)
            {
                throw new ArgumentNullException("operation");
            }

            try
            {
                TableResult result = await table.ExecuteAsync(operation);
                return result;
            }
            catch (Exception ex)
            {
                string errorMessage = GetStorageErrorMessage(ex);
                int statusCode = GetStorageStatusCode(ex);
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_OperationFailed, statusCode, errorMessage);
                _logger.LogError(msg, ex);

                return new TableResult { HttpStatusCode = statusCode };
            }
        }

        /// <inheritdoc />
        public async Task<IList<TableResult>> ExecuteBatchAsync(CloudTable table, TableBatchOperation batch)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }
            if (batch == null)
            {
                throw new ArgumentNullException("batch");
            }

            try
            {
                IList<TableResult> results = await table.ExecuteBatchAsync(batch);
                return results;
            }
            catch (Exception ex)
            {
                string errorMessage = GetStorageErrorMessage(ex);
                int statusCode = GetStorageStatusCode(ex);
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_OperationFailed, statusCode, errorMessage);
                _logger.LogError(msg, ex);
                return new List<TableResult>();
            }
        }

        /// <inheritdoc />
        public async Task<long> ExecuteDeleteAllAsync(CloudTable table, string partitionKey, string filter)
        {
            if (table == null)
            {
                throw new ArgumentNullException("table");
            }
            if (partitionKey == null)
            {
                throw new ArgumentNullException("partitionKey");
            }

            // Build query for retrieving exiting entries. We only ask for PK and RK.
            TableQuery query = new TableQuery()
            {
                FilterString = filter,
                SelectColumns = new List<string> { PartitionKey, RowKey },
            };
            AddPartitionKeyConstraint(query, partitionKey);

            try
            {
                long totalCount = 0;
                TableContinuationToken continuationToken = null;
                do
                {
                    DynamicTableEntity[] webHooks = (await ExecuteQueryAsync(table, query)).ToArray();
                    if (webHooks.Length == 0)
                    {
                        break;
                    }

                    // Delete query results in max of 100-count batches
                    int totalSegmentCount = webHooks.Length;
                    int segmentCount = 0;
                    do
                    {
                        TableBatchOperation batch = new TableBatchOperation();
                        int batchCount = Math.Min(totalSegmentCount - segmentCount, MaxBatchSize);
                        for (int cnt = 0; cnt < batchCount; cnt++)
                        {
                            DynamicTableEntity entity = webHooks[segmentCount + cnt];
                            entity.ETag = "*";
                            TableOperation operation = TableOperation.Delete(entity);
                            batch.Add(operation);
                        }

                        await ExecuteBatchAsync(table, batch);
                        segmentCount += batchCount;
                    }
                    while (segmentCount < totalSegmentCount);
                    totalCount += segmentCount;
                }
                while (continuationToken != null);
                return totalCount;
            }
            catch (Exception ex)
            {
                string errorMessage = GetStorageErrorMessage(ex);
                int statusCode = GetStorageStatusCode(ex);
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_OperationFailed, statusCode, errorMessage);
                _logger.LogError(msg, ex);
                throw new InvalidOperationException(msg, ex);
            }
        }

        /// <inheritdoc />
        public async Task AddMessagesAsync(CloudQueue queue, IEnumerable<CloudQueueMessage> messages)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            try
            {
                List<Task> addTasks = new List<Task>();
                foreach (var message in messages)
                {
                    Task addTask = queue.AddMessageAsync(message);
                    addTasks.Add(addTask);
                }

                await Task.WhenAll(addTasks);
            }
            catch (Exception ex)
            {
                string errorMessage = GetStorageErrorMessage(ex);
                int statusCode = GetStorageStatusCode(ex);
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_OperationFailed, statusCode, errorMessage);
                _logger.LogError(msg, ex);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(CloudQueue queue, int messageCount, TimeSpan timeout)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            try
            {
                IEnumerable<CloudQueueMessage> messages = await queue.GetMessagesAsync(messageCount, timeout, options: null, operationContext: null);
                return messages;
            }
            catch (Exception ex)
            {
                string errorMessage = GetStorageErrorMessage(ex);
                int statusCode = GetStorageStatusCode(ex);
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_OperationFailed, statusCode, errorMessage);
                _logger.LogError(msg, ex);
                return Enumerable.Empty<CloudQueueMessage>();
            }
        }

        /// <inheritdoc />
        public async Task DeleteMessagesAsync(CloudQueue queue, IEnumerable<CloudQueueMessage> messages)
        {
            if (queue == null)
            {
                throw new ArgumentNullException("queue");
            }

            try
            {
                List<Task> deleteTasks = new List<Task>();
                foreach (var message in messages)
                {
                    Task deleteTask = queue.DeleteMessageAsync(message);
                    deleteTasks.Add(deleteTask);
                }

                await Task.WhenAll(deleteTasks);
            }
            catch (Exception ex)
            {
                string errorMessage = GetStorageErrorMessage(ex);
                int statusCode = GetStorageStatusCode(ex);
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResource.StorageManager_OperationFailed, statusCode, errorMessage);
                _logger.LogError(msg, ex);
            }
        }

        /// <inheritdoc />
        public string GetStorageErrorMessage(Exception ex)
        {
            StorageException se = ex as StorageException;
            if (se != null && se.RequestInformation != null)
            {
                string status = se.RequestInformation.HttpStatusMessage != null ? se.RequestInformation.HttpStatusMessage + " " : string.Empty;
                string errorCode = se.RequestInformation.ExtendedErrorInformation != null ? "(" + se.RequestInformation.ExtendedErrorInformation.ErrorMessage + ")" : string.Empty;
                return status + errorCode;
            }
            else if (ex != null)
            {
                return ex.Message;
            }
            return string.Empty;
        }

        /// <inheritdoc />
        public int GetStorageStatusCode(Exception ex)
        {
            StorageException se = ex as StorageException;
            return se != null && se.RequestInformation != null ? se.RequestInformation.HttpStatusCode : 500;
        }

        internal static IStorageManager GetInstance(ILogger logger)
        {
            if (_storageManager != null)
            {
                return _storageManager;
            }

            IStorageManager instance = new StorageManager(logger);
            Interlocked.CompareExchange(ref _storageManager, instance, null);
            return _storageManager;
        }

        internal static void AddQueryConstraint(TableQuery query, string constraint)
        {
            query.FilterString = string.IsNullOrEmpty(query.FilterString)
                ? constraint
                : string.Format(CultureInfo.InvariantCulture, "({0}) and ({1})", query.FilterString, constraint);
        }

        internal static string GetLookupKey(string connectionString, string identifier)
        {
            string key = Hasher.GetFnvHash32AsString(connectionString) + "$" + identifier;
            return key;
        }
    }
}
