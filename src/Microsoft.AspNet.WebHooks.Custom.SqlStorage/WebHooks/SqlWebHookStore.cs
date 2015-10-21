using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNet.WebHooks.Diagnostics;
using Microsoft.AspNet.WebHooks.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.WebHooks
{
    /// <summary>
    /// Provides an implementation of <see cref="IWebHookStore"/> storing registered WebHooks in Microsoft SQL Server.
    /// </summary>
    public class SqlWebHookStore : IWebHookStore
    {
        internal const string SqlConnectionStringName = "WebHooksConnectionString";

        private readonly JsonSerializerSettings serializerSettings = 
            new JsonSerializerSettings() { Formatting = Formatting.None };
        private readonly WebHookContext context;
        private readonly IDataProtector protector;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlWebHookStore"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="protector">The protector.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">
        /// settings
        /// or
        /// protector
        /// or
        /// logger
        /// or
        /// </exception>
        public SqlWebHookStore(
            SettingsDictionary settings,
            IDataProtector protector,
            ILogger logger)
        {
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

            ConnectionSettings connection;
            if (!settings.Connections.TryGetValue(SqlConnectionStringName, out connection))
            {
                throw new ArgumentNullException(String.Format("The Setting [{0}] is missing from the Configuration", SqlConnectionStringName));
            }
            this.context = new WebHookContext(connection.ConnectionString);
            this.protector = protector;
            this.logger = logger;
        }


        public async Task DeleteAllWebHooksAsync(string user)
        {
            var matches = await context.Registrations.Where(r => r.User == user).ToArrayAsync();
            foreach(var m in matches)
            {
                context.Entry(m).State = EntityState.Deleted;
            }
            await context.SaveChangesAsync();
        }

        public async Task<StoreResult> DeleteWebHookAsync(string user, string id)
        {
            try
            {
                var match = await context.Registrations.Where(r => r.User == user && r.Id == id).FirstOrDefaultAsync();
                if (match == null)
                {
                    return StoreResult.NotFound;
                }
                context.Entry(match).State = EntityState.Deleted;
                await context.SaveChangesAsync();
            }
            catch (SqlException se)
            {
                logger.Error("Sql Error During Delete", se);
                return StoreResult.OperationError;
            }
            catch (Exception ex)
            {
                logger.Error("General Error During Delete", ex);
                return StoreResult.InternalError;
            }
            return StoreResult.Success;
        }

        public async  Task<ICollection<WebHook>> GetAllWebHooksAsync(string user)
        {
            var registrations = await context.Registrations.Where(r => r.User == user).ToArrayAsync();
            var collection = new List<WebHook>();
            foreach (var registration in registrations)
            {
                collection.Add(ConvertFromRegistration(registration));
            }
            return collection;
        }

        public async Task<StoreResult> InsertWebHookAsync(string user, WebHook webHook)
        {
            try
            {
                var registration = ConvertToRegistration(user, webHook);
                context.Registrations.Attach(registration);
                context.Entry(registration).State = EntityState.Added;
                await context.SaveChangesAsync();
            }
            catch (OptimisticConcurrencyException dbe)
            {
                logger.Error("Concurrency Error During Insert", dbe);
                return StoreResult.Conflict;
            }
            catch (SqlException se)
            {
                logger.Error("Sql Error During Insert", se);
                return StoreResult.OperationError;
            }
            catch (Exception ex)
            {
                logger.Error("General Error During Insert", ex);
                return StoreResult.InternalError;
            }
            return StoreResult.Success;
        }

        public async Task<WebHook> LookupWebHookAsync(string user, string id)
        {
            var registration = await context.Registrations.Where(r => r.User == user && r.Id == id).FirstOrDefaultAsync();
            if (registration != null)
            {
                return ConvertFromRegistration(registration);
            }
            return null;
        }

        public async Task<ICollection<WebHook>> QueryWebHooksAsync(string user, IEnumerable<string> actions)
        {
            var webHooks = await GetAllWebHooksAsync(user);
            ICollection<WebHook> matches = new List<WebHook>();
            foreach (WebHook webHook in webHooks)
            {
                if (webHook.IsPaused)
                {
                    continue;
                }

                foreach (string action in actions)
                {
                    if (webHook.MatchesAction(action))
                    {
                        matches.Add(webHook);
                        break;
                    }
                }
            }

            return matches;
        }

        public async Task<StoreResult> UpdateWebHookAsync(string user, WebHook webHook)
        {
            try
            {
                var registration = await context.Registrations.Where(r => r.User == user && r.Id == webHook.Id).FirstOrDefaultAsync();
                if (registration == null)
                {
                    return StoreResult.NotFound;
                }
                UpdateRegistrationFromWebHook(user, webHook, registration);
                context.Entry(registration).State = EntityState.Modified;
                await context.SaveChangesAsync();
            }
            catch (OptimisticConcurrencyException dbe)
            {
                logger.Error("Concurrency Error During Update", dbe);
                return StoreResult.Conflict;
            }
            catch (SqlException se)
            {
                logger.Error("Sql Error During Update", se);
                return StoreResult.OperationError;
            }
            catch (Exception ex)
            {
                logger.Error("General Error During Update", ex);
                return StoreResult.InternalError;
            }
            return StoreResult.Success;
        }

        private WebHook ConvertFromRegistration(Registration registration)
        {
            var data = protector.Unprotect(registration.ProtectedData);
            var webHook = JsonConvert.DeserializeObject<WebHook>(data, this.serializerSettings);
            return webHook;
        }

        private Registration ConvertToRegistration(string user, WebHook webHook)
        {
            var json = JsonConvert.SerializeObject(webHook, this.serializerSettings);
            var protectedData = protector.Protect(json);
            var registration = new Registration
            {
                User = user,
                Id = webHook.Id,
                ProtectedData = protectedData
            };
            return registration;
        }

        private void UpdateRegistrationFromWebHook(string user, WebHook webHook, Registration registration)
        {
            registration.User = user;
            registration.Id = webHook.Id;
            var json = JsonConvert.SerializeObject(webHook, this.serializerSettings);
            var protectedData = protector.Protect(json);
            registration.ProtectedData = protectedData;
        }
    }
}
