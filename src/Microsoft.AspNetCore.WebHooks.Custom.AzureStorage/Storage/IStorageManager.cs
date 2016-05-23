using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for accessing Microsoft Azure Table Storage.
    /// </summary>
    public interface IStorageManager
    {

        /// <summary>
        /// Gets a <see cref="CloudStorageAccount"/> given a <paramref name="connectionString"/>.
        /// </summary>
        CloudStorageAccount GetCloudStorageAccount(string connectionString);

        /// <summary>
        /// Gets a <see cref="CloudTable"/> given a <paramref name="connectionString"/> and <paramref name="tableName"/>.
        /// </summary>
        /// <returns>A new <see cref="CloudTable"/> instance.</returns>
        Task<CloudTable> GetCloudTableAsync(string connectionString, string tableName);

        /// <summary>
        /// Gets a <see cref="CloudQueue"/> given a <paramref name="connectionString"/> and <paramref name="queueName"/>.
        /// </summary>
        /// <returns>A new <see cref="CloudTable"/> instance.</returns>
        Task<CloudQueue> GetCloudQueueAsync(string connectionString, string queueName);

        /// <summary>
        /// Adds an explicit partition key constraint to an existing query.
        /// </summary>
        /// <param name="query">The current <see cref="TableQuery"/>.</param>
        /// <param name="partitionKey">The partitionKey to add the constraint for.</param>
        void AddPartitionKeyConstraint(TableQuery query, string partitionKey);

        /// <summary>
        /// Gets the value for an entity with a particular <paramref name="partitionKey"/> and <paramref name="rowKey"/>.
        /// </summary>
        /// <returns>The retrieval result.</returns>
        Task<TableResult> ExecuteRetrievalAsync(CloudTable table, string partitionKey, string rowKey);

        /// <summary>
        /// Executes a Table Storage query, logs any errors, and returns a collection with the resulting 
        /// <see cref="DynamicTableEntity"/> instances.
        /// </summary>
        Task<IEnumerable<DynamicTableEntity>> ExecuteQueryAsync(CloudTable table, TableQuery query);

        /// <summary>
        /// Executes a Table non-query operation, logs any errors and returns a <see cref="TableResult"/>. 
        /// Inspect the <see cref="TableResult"/> to see if the status code differs from 2xx meaning that the 
        /// operation failed.
        /// </summary>
        Task<TableResult> ExecuteAsync(CloudTable table, TableOperation operation);

        /// <summary>
        /// Executes a Table batched operation, logs any errors and returns a collection of <see cref="TableResult"/>. 
        /// Inspect the collection to see if the status code differs from 2xx meaning that the operation failed in
        /// some way.
        /// </summary>
        Task<IList<TableResult>> ExecuteBatchAsync(CloudTable table, TableBatchOperation batch);

        /// <summary>
        /// Deletes all entities with a given <paramref name="partitionKey"/> that matches the given <paramref name="filter"/>.
        /// If <paramref name="filter"/> is <c>null</c> then all entities with the partition key are deleted.
        /// </summary>
        /// <returns>The number of entities deleted.</returns>
        Task<long> ExecuteDeleteAllAsync(CloudTable table, string partitionKey, string filter);

        /// <summary>
        /// Inserts the given <paramref name="messages"/> to the given <paramref name="queue"/>.
        /// </summary>
        /// <param name="queue">The <see cref="CloudQueue"/> to insert into the queue.</param>
        /// <param name="messages">The messages to add.</param>
        Task AddMessagesAsync(CloudQueue queue, IEnumerable<CloudQueueMessage> messages);

        /// <summary>
        /// Gets messages from the given <paramref name="queue"/>. 
        /// </summary>
        /// <param name="queue">The <see cref="CloudQueue"/> to retrieve the messages from.</param>
        /// <param name="messageCount">The number of messages to retrieve.</param>
        /// <param name="timeout">A <see cref="TimeSpan"/> specifying the visibility timeout.</param>
        /// <returns>A collection of retrieved messages.</returns>
        Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(CloudQueue queue, int messageCount, TimeSpan timeout);

        /// <summary>
        /// Deletes the given <paramref name="messages"/> from the given <paramref name="queue"/>.
        /// </summary>
        /// <param name="queue">The <see cref="CloudQueue"/> to delete the messages from.</param>
        /// <param name="messages">The messages to delete.</param>
        Task DeleteMessagesAsync(CloudQueue queue, IEnumerable<CloudQueueMessage> messages);

        /// <summary>
        /// Gets the extended error message from a <see cref="StorageException"/> or the Message information from any other <see cref="Exception"/> type.
        /// </summary>
        /// <param name="ex">The exception to extract message from.</param>
        /// <returns>The exception message.</returns>
        string GetStorageErrorMessage(Exception ex);

        /// <summary>
        /// Gets the status code from a <see cref="StorageException"/>.
        /// </summary>
        /// <param name="ex">The exception to extract message from.</param>
        /// <returns>The HTTP status code.</returns>
        int GetStorageStatusCode(Exception ex);
    }
}
