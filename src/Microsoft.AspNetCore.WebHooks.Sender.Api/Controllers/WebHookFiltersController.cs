using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks.Routes;

namespace Microsoft.AspNetCore.WebHooks.Controllers
{
    /// <summary>
    /// The <see cref="WebHookRegistrationsController"/> allows the caller to get the list of filters 
    /// with which a WebHook can be registered. This enables a client to provide a user experience
    /// indicating which filters can be used when registering a <see cref="WebHook"/>. 
    /// </summary>
    [Authorize]
    [Route("api/webhooks/filters")]
    public class WebHookFiltersController : ControllerBase
    {
        private readonly IWebHookFilterManager _filterManager;

        /// <inheritdoc />
        public WebHookFiltersController(IWebHookFilterManager filterManager)
        {
            _filterManager = filterManager;
        }

        /// <summary>
        /// Gets all WebHook filters that a user can register with. The filters indicate which WebHook
        /// events that this WebHook will be notified for.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the operation.</returns>
        [Route("", Name = WebHookRouteNames.FiltersGetAction)]
        public async Task<IEnumerable<WebHookFilter>> Get()
        {
            var filters = await _filterManager.GetAllWebHookFiltersAsync();
            return filters.Values;
        }
    }
}
