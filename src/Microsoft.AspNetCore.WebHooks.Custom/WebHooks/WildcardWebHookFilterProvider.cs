using Microsoft.AspNetCore.WebHooks.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Defines a default wildcard <see cref="WebHookFilter"/> which matches all filters.
    /// </summary>
    public class WildcardWebHookFilterProvider : IWebHookFilterProvider
    {
        private const string WildcardName = "*";

        private static readonly Collection<WebHookFilter> Filters = new Collection<WebHookFilter>
        {
            new WebHookFilter { Name = WildcardName, Description = CustomResource.Filter_WildcardDescription },
        };

        /// <summary>
        /// Gets the name of the <see cref="WebHookFilter"/> registered by this <see cref="IWebHookFilterProvider"/>.
        /// </summary>
        public static string Name
        {
            get { return WildcardName; }
        }

        /// <inheritdoc />
        public Task<Collection<WebHookFilter>> GetFiltersAsync()
        {
            return Task.FromResult(Filters);
        }
    }
}
