using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// List of WebHook Options that can be set during startup
    /// </summary>
    public class WebHookOptions
    {
        Type _store;
        Type _sender;
        IList<Type> _filters;

        /// <summary>
        /// The Selected Store Type
        /// </summary>
        internal Type StoreType { get { return _store; } }

        /// <summary>
        /// The Selected Sender Type
        /// </summary>
        internal Type SenderType { get { return _sender; } }

        /// <summary>
        /// Creates a new <see cref="WebHookOptions"/>
        /// </summary>
        public WebHookOptions()
        {
            // Default Values go here
            _store = typeof(MemoryWebHookStore);
            _sender = typeof(DataflowWebHookSender);
            _filters = new List<Type>();

            // Add Special / Default Filter Provider
            AddFilterProvider<WildcardWebHookFilterProvider>();
        }

        /// <summary>
        /// Sets the <see cref="IWebHookStore"/> to be something different
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UseStore<T>() where T : IWebHookStore
        {
            _store = typeof(T);
        }

        /// <summary>
        /// Sets the <see cref="IWebHookSender"/> to be something different
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void UseSender<T>() where T : IWebHookSender
        {
            _sender = typeof(T);
        }

        /// <summary>
        /// Registers a <see cref="IWebHookFilterProvider"/> to Provide WebHook Filters
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void AddFilterProvider<T>() where T : IWebHookFilterProvider
        {
            _filters.Add(typeof(T));
        }
    }
}
