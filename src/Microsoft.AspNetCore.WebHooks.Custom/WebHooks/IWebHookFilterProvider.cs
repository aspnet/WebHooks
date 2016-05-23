using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Provides an abstraction for adding filters that can be used to determine when <see cref="WebHook"/> are triggered.
    /// </summary>
    public interface IWebHookFilterProvider
    {
        /// <summary>
        /// Get the filters for this <see cref="IWebHookFilterProvider"/> implementation so that they be applied to <see cref="WebHook"/>
        /// instances.
        /// </summary>
        /// <returns>A collection of <see cref="WebHookFilter"/> instances.</returns>
        Task<Collection<WebHookFilter>> GetFiltersAsync();
    }
}
