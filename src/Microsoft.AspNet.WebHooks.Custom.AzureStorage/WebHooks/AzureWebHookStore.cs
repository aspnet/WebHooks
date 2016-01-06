﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNet.WebHooks.Diagnostics;
using Microsoft.AspNet.WebHooks.Properties;
using Microsoft.AspNet.WebHooks.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Microsoft.AspNet.WebHooks
{
    /// <summary>
    /// Provides an implementation of <see cref="IWebHookStore"/> storing registered WebHooks in Microsoft Azure Table Storage.
    /// </summary>
    [CLSCompliant(false)]
    public class AzureWebHookStore : WebHookStore
    {
        internal const string WebHookTable = "WebHooks";
        internal const string WebHookDataColumn = "Data";
        internal const int AzureStoreSecretKeyMinLength = 8;
        internal const int AzureStoreSecretKeyMaxLength = 64;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings() { Formatting = Formatting.None };
        private readonly IStorageManager _manager;
        private readonly IDataProtector _protector;
        private readonly ILogger _logger;
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureWebHookStore"/> class with the given <paramref name="manager"/>,
        /// <paramref name="settings"/>, <paramref name="protector"/>, and <paramref name="logger"/>.
        /// </summary>
        public AzureWebHookStore(IStorageManager manager, SettingsDictionary settings, IDataProtector protector, ILogger logger)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }
            if (protector == null)
            {
                throw new ArgumentNullException("protector");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _manager = manager;
            _connectionString = manager.GetAzureStorageConnectionString(settings);
            _protector = protector;
            _logger = logger;
        }

        /// <inheritdoc />
        public override async Task<ICollection<WebHook>> GetAllWebHooksAsync(string user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user = NormalizeKey(user);

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            TableQuery query = new TableQuery();
            _manager.AddPartitionKeyConstraint(query, user);

            IEnumerable<DynamicTableEntity> entities = await _manager.ExecuteQueryAsync(table, query);
            ICollection<WebHook> result = entities.Select(e => ConvertToWebHook(e))
                .Where(w => w != null)
                .ToArray();
            return result;
        }

        /// <inheritdoc />
        public override async Task<ICollection<WebHook>> QueryWebHooksAsync(string user, IEnumerable<string> actions, Func<WebHook, string, bool> predicate)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            user = NormalizeKey(user);

            predicate = predicate ?? DefaultPredicate;

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            TableQuery query = new TableQuery();
            _manager.AddPartitionKeyConstraint(query, user);

            IEnumerable<DynamicTableEntity> entities = await _manager.ExecuteQueryAsync(table, query);
            ICollection<WebHook> matches = entities.Select(e => ConvertToWebHook(e))
                .Where(w => MatchesAnyAction(w, actions) && predicate(w, user))
                .ToArray();
            return matches;
        }

        /// <inheritdoc />
        public override async Task<WebHook> LookupWebHookAsync(string user, string id)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            user = NormalizeKey(user);
            id = NormalizeKey(id);

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            TableResult result = await _manager.ExecuteRetrievalAsync(table, user, id);
            if (!result.IsSuccess())
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResources.AzureStore_NotFound, user, id);
                _logger.Info(msg);
                return null;
            }

            DynamicTableEntity entity = result.Result as DynamicTableEntity;
            return ConvertToWebHook(entity);
        }

        /// <inheritdoc />
        public override async Task<StoreResult> InsertWebHookAsync(string user, WebHook webHook)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (webHook == null)
            {
                throw new ArgumentNullException("webHook");
            }

            user = NormalizeKey(user);
            string id = NormalizeKey(webHook.Id);

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            DynamicTableEntity tableEntity = ConvertFromWebHook(user, id, webHook);
            TableOperation operation = TableOperation.Insert(tableEntity, echoContent: false);
            TableResult tableResult = await _manager.ExecuteAsync(table, operation);

            StoreResult result = GetStoreResult(tableResult);
            if (result != StoreResult.Success)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResources.StorageManager_CreateFailed, table.Name, tableResult.HttpStatusCode);
                _logger.Error(msg);
            }
            return result;
        }

        /// <inheritdoc />
        public override async Task<StoreResult> UpdateWebHookAsync(string user, WebHook webHook)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (webHook == null)
            {
                throw new ArgumentNullException("webHook");
            }

            user = NormalizeKey(user);
            string id = NormalizeKey(webHook.Id);

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            DynamicTableEntity tableEntity = ConvertFromWebHook(user, id, webHook);
            TableOperation operation = TableOperation.Replace(tableEntity);
            TableResult tableResult = await _manager.ExecuteAsync(table, operation);

            StoreResult result = GetStoreResult(tableResult);
            if (result != StoreResult.Success)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResources.StorageManager_OperationFailed, table.Name, tableResult.HttpStatusCode);
                _logger.Error(msg);
            }
            return result;
        }

        /// <inheritdoc />
        public override async Task<StoreResult> DeleteWebHookAsync(string user, string id)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            user = NormalizeKey(user);
            id = NormalizeKey(id);

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            TableEntity tableEntity = new TableEntity(user, id);
            tableEntity.ETag = "*";

            TableOperation operation = TableOperation.Delete(tableEntity);
            TableResult tableResult = await _manager.ExecuteAsync(table, operation);

            StoreResult result = GetStoreResult(tableResult);
            if (result != StoreResult.Success)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResources.StorageManager_OperationFailed, table.Name, tableResult.HttpStatusCode);
                _logger.Error(msg);
            }
            return result;
        }

        /// <inheritdoc />
        public override async Task DeleteAllWebHooksAsync(string user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user = NormalizeKey(user);

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            await _manager.ExecuteDeleteAllAsync(table, user, filter: null);
        }

        /// <inheritdoc />
        public override async Task<ICollection<WebHook>> QueryWebHooksAcrossAllUsersAsync(IEnumerable<string> actions, Func<WebHook, string, bool> predicate)
        {
            if (actions == null)
            {
                throw new ArgumentNullException("actions");
            }

            CloudTable table = _manager.GetCloudTable(_connectionString, WebHookTable);
            TableQuery query = new TableQuery();

            predicate = predicate ?? DefaultPredicate;

            IEnumerable<DynamicTableEntity> entities = await _manager.ExecuteQueryAsync(table, query);
            var matches = new List<WebHook>();
            foreach (var entity in entities)
            {
                WebHook webHook = ConvertToWebHook(entity);
                if (MatchesAnyAction(webHook, actions) && predicate(webHook, entity.PartitionKey))
                {
                    matches.Add(webHook);
                }
            }
            return matches;
        }

        private static bool DefaultPredicate(WebHook webHook, string user)
        {
            return true;
        }

        private static StoreResult GetStoreResult(TableResult result)
        {
            if (result.IsSuccess())
            {
                return StoreResult.Success;
            }
            if (result.IsNotFound())
            {
                return StoreResult.NotFound;
            }
            if (result.IsConflict())
            {
                return StoreResult.Conflict;
            }
            if (result.IsServerError())
            {
                return StoreResult.InternalError;
            }
            return StoreResult.OperationError;
        }

        private WebHook ConvertToWebHook(DynamicTableEntity entity)
        {
            EntityProperty property;
            if (entity == null || !entity.Properties.TryGetValue(WebHookDataColumn, out property))
            {
                return null;
            }

            try
            {
                string encryptedContent = property.StringValue;
                if (encryptedContent != null)
                {
                    string content = _protector.Unprotect(encryptedContent);
                    WebHook webHook = JsonConvert.DeserializeObject<WebHook>(content, _serializerSettings);
                    return webHook;
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, AzureStorageResources.AzureStore_BadWebHook, typeof(WebHook).Name, ex.Message);
                _logger.Error(msg, ex);
            }
            return null;
        }

        private DynamicTableEntity ConvertFromWebHook(string partitionKey, string rowKey, WebHook webHook)
        {
            DynamicTableEntity entity = new DynamicTableEntity(partitionKey, rowKey);
            entity.ETag = "*";

            // Set data column with encrypted serialization of WebHook
            string content = JsonConvert.SerializeObject(webHook, _serializerSettings);
            string encryptedContent = _protector.Protect(content);
            EntityProperty property = EntityProperty.GeneratePropertyForString(encryptedContent);
            entity.Properties.Add(WebHookDataColumn, property);

            return entity;
        }
    }
}
