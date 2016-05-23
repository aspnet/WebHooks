using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks.Receivers
{
    /// <summary>
    /// Stores Options and Configuration for WebHook Receivers
    /// </summary>
    public class CustomWebHookReceiverOptions
    {
        IDictionary<string, string> _secretKeys;

        public CustomWebHookReceiverOptions()
        {
            _secretKeys = new Dictionary<string, string>();
        }

        public string GetReceiverConfig(string name, string id)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return _secretKeys[name];
            }
            else
            {
                return _secretKeys[CompositeKey(name, id)];
            }
        }

        public CustomWebHookReceiverOptions AddKey(string name, string secret)
        {
            _secretKeys.Add(name, secret);
            return this;
        }

        public CustomWebHookReceiverOptions AddKey(string name, string id, string secret)
        {
            _secretKeys.Add(CompositeKey(name, id), secret);
            return this;
        }

        private string CompositeKey(string name, string id)
        {
            if (String.IsNullOrWhiteSpace(id))
                return name;
            return $"{name}/{id}";
        }
    }
}
