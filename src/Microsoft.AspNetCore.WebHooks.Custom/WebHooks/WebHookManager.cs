using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.AspNetCore.WebHooks.Properties;
using System.Globalization;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an implementation of <see cref="IWebHookManager"/> for managing notifications and mapping
    /// them to registered WebHooks.
    /// </summary>
    public class WebHookManager : IWebHookManager, IDisposable
    {
        internal const string EchoParameter = "echo";

        private readonly IWebHookStore _webHookStore;
        private readonly IWebHookSender _webHookSender;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        private bool _disposed;

        /// <summary>
        /// Initialize a new instance of the <see cref="WebHookManager"/> with a default retry policy.
        /// </summary>
        /// <param name="webHookStore">The current <see cref="IWebHookStore"/>.</param>
        /// <param name="webHookSender">The current <see cref="IWebHookSender"/>.</param>
        /// <param name="logger">The current <see cref="ILogger"/>.</param>
        public WebHookManager(IWebHookStore webHookStore, IWebHookSender webHookSender, ILogger<WebHookManager> logger)
        {
            if (webHookStore == null)
            {
                throw new ArgumentNullException("webHookStore");
            }
            if (webHookSender == null)
            {
                throw new ArgumentNullException("webHookSender");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _webHookStore = webHookStore;
            _webHookSender = webHookSender;
            _logger = logger;

            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task VerifyWebHookAsync(WebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException("webHook");
            }

            // Check that we have a valid secret
            if (string.IsNullOrEmpty(webHook.Secret) || webHook.Secret.Length < 32 || webHook.Secret.Length > 64)
            {
                throw new InvalidOperationException(CustomResource.WebHook_InvalidSecret);
            }

            // Check that WebHook URI is either 'http' or 'https'
            if (!(webHook.WebHookUri.Scheme.Equals("http", StringComparison.CurrentCultureIgnoreCase) || webHook.WebHookUri.Scheme.Equals("https", StringComparison.CurrentCultureIgnoreCase)))
            {
                string msg = string.Format(CultureInfo.CurrentCulture, CustomResource.Manager_NoHttpUri, webHook.WebHookUri);
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            // Create the echo query parameter that we want returned in response body as plain text.
            string echo = Guid.NewGuid().ToString("N");

            HttpResponseMessage response;
            try
            {
                // Get request URI with echo query parameter
                UriBuilder webHookUri = new UriBuilder(webHook.WebHookUri);
                webHookUri.Query = EchoParameter + "=" + echo;

                // Create request adding any additional request headers (not entity headers) from Web Hook
                HttpRequestMessage hookRequest = new HttpRequestMessage(HttpMethod.Get, webHookUri.Uri);
                foreach (var kvp in webHook.Headers)
                {
                    hookRequest.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }

                response = await _httpClient.SendAsync(hookRequest);
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, CustomResource.Manager_VerifyFailure, ex.Message);
                _logger.LogError(msg, ex);
                throw new InvalidOperationException(msg);
            }

            if (!response.IsSuccessStatusCode)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, CustomResource.Manager_VerifyFailure, response.StatusCode);
                _logger.LogInformation(msg);
                throw new InvalidOperationException(msg);
            }

            // Verify response body
            if (response.Content == null)
            {
                string msg = CustomResource.Manager_VerifyNoBody;
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            string actualEcho = await response.Content.ReadAsStringAsync();
            if (!string.Equals(actualEcho, echo, StringComparison.Ordinal))
            {
                string msg = CustomResource.Manager_VerifyBadEcho;
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
        }

        /// <inheritdoc />
        public async Task<int> NotifyAsync(string user, IEnumerable<IWebHookNotification> notifications, Func<WebHook, string, bool> predicate)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            if (notifications == null)
            {
                throw new ArgumentNullException("notifications");
            }

            // Get all actions in this batch
            ICollection<IWebHookNotification> nots = notifications.ToArray();
            string[] actions = nots.Select(n => n.Action).ToArray();

            // Find all active WebHooks that matches at least one of the actions
            ICollection<WebHook> webHooks = await _webHookStore.QueryWebHooksAsync(user, actions, predicate);

            // For each WebHook set up a work item with the right set of notifications
            IEnumerable<WebHookWorkItem> workItems = GetWorkItems(webHooks, nots);

            // Start sending WebHooks
            await _webHookSender.SendWebHookWorkItemsAsync(workItems);
            return webHooks.Count;
        }

        /// <inheritdoc />
        public async Task<int> NotifyAllAsync(IEnumerable<IWebHookNotification> notifications, Func<WebHook, string, bool> predicate)
        {
            if (notifications == null)
            {
                throw new ArgumentNullException("notifications");
            }

            // Get all actions in this batch
            ICollection<IWebHookNotification> nots = notifications.ToArray();
            string[] actions = nots.Select(n => n.Action).ToArray();

            // Find all active WebHooks that matches at least one of the actions
            ICollection<WebHook> webHooks = await _webHookStore.QueryWebHooksAcrossAllUsersAsync(actions, predicate);

            // For each WebHook set up a work item with the right set of notifications
            IEnumerable<WebHookWorkItem> workItems = GetWorkItems(webHooks, nots);

            // Start sending WebHooks
            await _webHookSender.SendWebHookWorkItemsAsync(workItems);
            return webHooks.Count;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal static IEnumerable<WebHookWorkItem> GetWorkItems(ICollection<WebHook> webHooks, ICollection<IWebHookNotification> notifications)
        {
            List<WebHookWorkItem> workItems = new List<WebHookWorkItem>();
            foreach (WebHook webHook in webHooks)
            {
                ICollection<IWebHookNotification> webHookNotifications;

                // Pick the notifications that apply for this particular WebHook. If we only got one notification
                // then we know that it applies to all WebHooks. Otherwise each notification may apply only to a subset.
                if (notifications.Count == 1)
                {
                    webHookNotifications = notifications;
                }
                else
                {
                    webHookNotifications = notifications.Where(n => webHook.MatchesAction(n.Action)).ToArray();
                    if (webHookNotifications.Count == 0)
                    {
                        continue;
                    }
                }

                WebHookWorkItem workItem = new WebHookWorkItem(webHook, webHookNotifications);
                workItems.Add(workItem);
            }
            return workItems;
        }

        /// <summary>
        /// Releases the unmanaged resources and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <b>false</b> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    if (_httpClient != null)
                    {
                        _httpClient.Dispose();
                    }
                }
            }
        }
    }
}
