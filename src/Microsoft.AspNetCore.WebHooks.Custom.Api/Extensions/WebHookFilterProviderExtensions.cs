using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Extension Methods for Filter Providers
    /// </summary>
    public static  class WebHookFilterProviderExtensions
    {

        /// <summary>
        /// Gets a <see cref="Dictionary{TKey,TValue}"/> of all registered <see cref="WebHookFilter"/> instances 
        /// provided by registered <see cref="IWebHookFilterProvider"/> instances.
        /// </summary>
        /// <returns>An <see cref="IDictionary{TKey, TValue}"/> of <see cref="WebHookFilter"/> instances keyed by name.</returns>
        public static async Task<IDictionary<string, WebHookFilter>> GetAllWebHookFiltersAsync(this IEnumerable<IWebHookFilterProvider> providers)
        {
            IDictionary<string, WebHookFilter> allFilters = new Dictionary<string, WebHookFilter>(StringComparer.OrdinalIgnoreCase);

            // Get all filters from all providers.
            IEnumerable<Task<Collection<WebHookFilter>>> tasks = providers.Select(p => p.GetFiltersAsync());
            Collection<WebHookFilter>[] providerFilters = await Task.WhenAll(tasks);

            // Flatten filters into one dictionary for lookup.
            foreach (Collection<WebHookFilter> providerFilter in providerFilters)
            {
                foreach (WebHookFilter filter in providerFilter)
                {
                    allFilters[filter.Name] = filter;
                }
            }

            return allFilters;
        }
    }
}
