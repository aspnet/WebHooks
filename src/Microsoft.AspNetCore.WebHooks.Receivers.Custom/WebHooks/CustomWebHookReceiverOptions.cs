using System.Collections.Generic;

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

        public string GetReceiverConfig(string name)
        {
            return _secretKeys[name];
        }

        public CustomWebHookReceiverOptions AddSecret(string name, string secret)
        {
            _secretKeys.Add(name, secret);
            return this;
        }
    }
}
