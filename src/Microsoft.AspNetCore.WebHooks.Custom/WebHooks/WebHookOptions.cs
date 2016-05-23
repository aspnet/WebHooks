using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class WebHookOptions
    {
        Type _store;
        Type _sender;
        IList<Type> _filters;

        internal Type StoreType { get { return _store; } }

        internal Type SenderType { get { return _sender; } }

        public WebHookOptions()
        {
            // Default Values go here
            _store = typeof(MemoryWebHookStore);
            _sender = typeof(DataflowWebHookSender);
            _filters = new List<Type>();

            // Add Special / Default Filter Provider
            AddFilterProvider<WildcardWebHookFilterProvider>();
        }

        public void UseStore<T>() where T : IWebHookStore
        {
            _store = typeof(T);
        }

        public void UseSender<T>() where T : IWebHookStore
        {
            _sender = typeof(T);
        }

        public void AddFilterProvider<T>() where T : IWebHookFilterProvider
        {
            _filters.Add(typeof(T));
        }
    }
}
