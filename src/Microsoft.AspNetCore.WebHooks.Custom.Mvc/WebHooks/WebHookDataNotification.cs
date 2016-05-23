using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// A <see cref="IWebHookNotification"/> With a Generic Set of Data
    /// </summary>
    public class WebHookDataNotification : Dictionary<string, object>, IWebHookNotification
    {

        /// <summary>
        /// Creates a Generic <see cref="IWebHookNotification"/> with an Action and Data
        /// </summary>
        /// <param name="action">The action occuring with the webhook</param>
        /// <param name="data">Any data to pass along with the webhook notification</param>
        public WebHookDataNotification(string action, object data)
        {
            IDictionary<string, object> dataAsDictionary = data as IDictionary<string, object>;
            if (dataAsDictionary == null && data != null)
            {
                dataAsDictionary = new Dictionary<string, object>();
                PropertyInfo[] properties = data.GetType().GetTypeInfo().GetProperties();
                foreach (PropertyInfo prop in properties)
                {
                    object val = prop.GetValue(data);
                    dataAsDictionary.Add(prop.Name, val);
                }
            }

            if (dataAsDictionary != null)
            {
                foreach (KeyValuePair<string, object> item in dataAsDictionary)
                {
                    this[item.Key] = item.Value;
                }
            }
            Action = action;
        }

        /// <inheritdoc />
        public string Action { get; set; }
    }
}
