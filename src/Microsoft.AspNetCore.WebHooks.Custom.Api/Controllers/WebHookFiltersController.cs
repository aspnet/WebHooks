using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// The <see cref="WebHookRegistrationsController"/> allows the caller to get the list of filters 
    /// with which a WebHook can be registered. This enables a client to provide a user experience
    /// indicating which filters can be used when registering a <see cref="WebHook"/>. 
    /// </summary>
    [Authorize]
    [Route("api/webhooks/filters")]
    public class WebHookFiltersController : Controller
    {
        private readonly IEnumerable<IWebHookFilterProvider> _providers;

        /// <summary>
        /// Constructs a <see cref="WebHookFiltersController"/>.
        /// </summary>
        /// <param name="providers">Injected set of <see cref="IWebHookFilterProvider"/>s.</param>
        public WebHookFiltersController(IEnumerable<IWebHookFilterProvider> providers)
        {
            _providers = providers;
        }

        /// <summary>
        /// Gets all WebHook filters that a user can register with. The filters indicate which WebHook
        /// events that this WebHook will be notified for.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [HttpGet]
        public async Task<IEnumerable<WebHookFilter>> Get()
        {
            List<WebHookFilter> allfilters = new List<WebHookFilter>();
            foreach(IWebHookFilterProvider provider in _providers)
            {
                Collection<WebHookFilter> filters = await provider.GetFiltersAsync();
                foreach (WebHookFilter filter in filters)
                {
                    allfilters.Add(filter);
                }
            }
            return allfilters;
        }
    }
}
