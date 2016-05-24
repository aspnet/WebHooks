using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// The <see cref="WebHookRegistrationsController"/> allows the caller to create, modify, and manage WebHooks
    /// through a REST-style interface.
    /// </summary>
    [Authorize]
    [Route("api/webhooks/registrations")]
    public class WebHookRegistrationsController : Controller
    {
        private readonly IWebHookManager _manager;
        private readonly IWebHookStore _store;
        private readonly ILogger _logger;
        private readonly IEnumerable<IWebHookFilterProvider> _providers;

        public WebHookRegistrationsController(IWebHookManager manager, IWebHookStore store, 
            ILogger<WebHookRegistrationsController> logger, 
            IEnumerable<IWebHookFilterProvider> providers)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (providers == null)
            {
                throw new ArgumentNullException("providers");
            }

            _manager = manager;
            _store = store;
            _logger = logger;
            _providers = providers;
        }

        /// <summary>
        /// Gets all registered WebHooks for a given user.
        /// </summary>
        /// <returns>A collection containing the registered <see cref="WebHook"/> instances for a given user.</returns>
        [HttpGet]
        public async Task<IEnumerable<WebHook>> Get()
        {
            string userId = User.Identity.Name;
            IEnumerable<WebHook> webHooks = await _store.GetAllWebHooksAsync(userId);
            RemovePrivateFilters(webHooks);
            return webHooks;
        }

        /// <summary>
        /// Looks up a registered WebHooks with the given <paramref name="id"/> for a given user.
        /// </summary>
        /// <returns>The registered <see cref="WebHook"/> instance for a given user.</returns>
        [HttpGet("{id}", Name = WebHookRouteNames.RegistrationLookupAction)]
        public async Task<IActionResult> Lookup(string id)
        {
            string userId = User.Identity.Name;
            WebHook webHook = await _store.LookupWebHookAsync(userId, id);
            if (webHook != null)
            {
                RemovePrivateFilters(new[] { webHook });
                return new ObjectResult(webHook);
            }
            return NotFound();
        }

        /// <summary>
        /// Registers a new WebHook for a given user.
        /// </summary>
        /// <param name="webHook">The <see cref="WebHook"/> to create.</param>
        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> Post([FromBody] WebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException("webHook");
            }

            string userId = User.Identity.Name;
            await VerifyFilters(webHook);
            try
            {
                // Validate the WebHook is Active (using echo)
                await _manager.VerifyWebHookAsync(webHook);

                // Validate the provided WebHook ID (or force one to be created on server side)
                IWebHookIdValidator idValidator = (IWebHookIdValidator) HttpContext.RequestServices.GetService(typeof(IWebHookIdValidator));
                if (idValidator == null)
                {
                    idValidator = new DefaultWebHookIdValidator();
                }
                await idValidator.ValidateIdAsync(Request, webHook);

                // Add WebHook for this user.
                StoreResult result = await _store.InsertWebHookAsync(userId, webHook);
                if (result == StoreResult.Success)
                {
                    return CreatedAtRoute(WebHookRouteNames.RegistrationLookupAction, new { id = webHook.Id }, webHook);
                }
                return CreateResultFromStoreResult(result);
            }
            catch (Exception ex)
            {
                string msg = string.Format(CustomApiResource.RegistrationController_RegistrationFailure, ex.Message);
                _logger.LogError(msg);
                return StatusCode(StatusCodes.Status500InternalServerError, msg);
            }
        }

        /// <summary>
        /// Updates an existing WebHook registration.
        /// </summary>
        /// <param name="id">The WebHook ID.</param>
        /// <param name="webHook">The new <see cref="WebHook"/> to use.</param>
        [HttpPut("{id}")]
        [ValidateModel]
        public async Task<IActionResult> Put(string id, [FromBody] WebHook webHook)
        {
            if (webHook == null)
            {
                return BadRequest();
            }
            if (!string.Equals(id, webHook.Id, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            string userId = User.Identity.Name;
            await VerifyFilters(webHook);

            try
            {
                // Validate the WebHook is Active (using echo)
                await _manager.VerifyWebHookAsync(webHook);

                // Validate the provided WebHook ID (or force one to be created on server side)
                IWebHookIdValidator idValidator = (IWebHookIdValidator)HttpContext.RequestServices.GetService(typeof(IWebHookIdValidator));
                if (idValidator == null)
                {
                    idValidator = new DefaultWebHookIdValidator();
                }
                await idValidator.ValidateIdAsync(Request, webHook);

                StoreResult result = await _store.UpdateWebHookAsync(userId, webHook);
                return CreateResultFromStoreResult(result);
            }
            catch (Exception ex)
            {
                string msg = string.Format(CustomApiResource.RegistrationController_UpdateFailure, ex.Message);
                _logger.LogError(msg);
                return StatusCode(StatusCodes.Status500InternalServerError, msg);
            }
        }

        /// <summary>
        /// Deletes an existing WebHook registration.
        /// </summary>
        /// <param name="id">The WebHook ID.</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            string userId = User.Identity.Name;

            try
            {
                StoreResult result = await _store.DeleteWebHookAsync(userId, id);
                return CreateResultFromStoreResult(result);
            }
            catch (Exception ex)
            {
                string msg = string.Format(CustomApiResource.RegistrationController_DeleteFailure, ex.Message);
                _logger.LogError(msg);
                return StatusCode(StatusCodes.Status500InternalServerError, msg);
            }
        }

        /// <summary>
        /// Deletes all existing WebHook registrations.
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            string userId = User.Identity.Name;

            try
            {
                await _store.DeleteAllWebHooksAsync(userId);
                return Ok();
            }
            catch (Exception ex)
            {
                string msg = string.Format(CustomApiResource.RegistrationController_DeleteAllFailure, ex.Message);
                _logger.LogError(msg);
                return StatusCode(StatusCodes.Status500InternalServerError, msg);
            }
        }


        /// <summary>
        /// Removes all private filters from registered WebHooks.
        /// </summary>
        protected virtual void RemovePrivateFilters(IEnumerable<WebHook> webHooks)
        {
            if (webHooks == null)
            {
                throw new ArgumentNullException("webHooks");
            }

            foreach (WebHook webHook in webHooks)
            {
                var filters = webHook.Filters.Where(f => f.StartsWith(WebHookRegistrar.PrivateFilterPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
                foreach (string filter in filters)
                {
                    webHook.Filters.Remove(filter);
                }
            }
        }

        /// <summary>
        /// Ensure that the provided <paramref name="webHook"/> only has registered filters.
        /// </summary>
        protected virtual async Task VerifyFilters(WebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException("webHook");
            }

            // If there are no filters then add our wildcard filter.
            if (webHook.Filters.Count == 0)
            {
                webHook.Filters.Add(WildcardWebHookFilterProvider.Name);
                await InvokeRegistrars(webHook);
                return;
            }

            IDictionary<string, WebHookFilter> filters = await _providers.GetAllWebHookFiltersAsync();
            HashSet<string> normalizedFilters = new HashSet<string>();
            List<string> invalidFilters = new List<string>();
            foreach (string filter in webHook.Filters)
            {
                WebHookFilter hookFilter;
                if (filters.TryGetValue(filter, out hookFilter))
                {
                    normalizedFilters.Add(hookFilter.Name);
                }
                else
                {
                    invalidFilters.Add(filter);
                }
            }

            if (invalidFilters.Count > 0)
            {
                string invalidFiltersMsg = string.Join(", ", invalidFilters);
                string link = Url.Link(WebHookRouteNames.FiltersGetAction, values: null);
                string msg = string.Format(CustomApiResource.RegistrationController_InvalidFilters, invalidFiltersMsg, link);
                _logger.LogInformation(msg);

                throw new Exception(msg);
            }
            else
            {
                webHook.Filters.Clear();
                foreach (string filter in normalizedFilters)
                {
                    webHook.Filters.Add(filter);
                }
            }

            await InvokeRegistrars(webHook);
        }


        /// <summary>
        /// Calls all IWebHookRegistrar instances for server side manipulation, inspection, or rejection of registrations.
        /// </summary>
        private async Task InvokeRegistrars(WebHook webHook)
        {
            IEnumerable<IWebHookRegistrar> _registrars = 
                (IEnumerable<IWebHookRegistrar>)HttpContext.RequestServices.GetService(typeof(IEnumerable<IWebHookRegistrar>));

            if (_registrars == null || _registrars.Count() == 0)
            {
                return;
            }

            foreach (IWebHookRegistrar registrar in _registrars)
            {
                try
                {
                    await registrar.RegisterAsync(Request, webHook);
                }
                catch (Exception ex)
                {
                    string msg = string.Format(CustomApiResource.RegistrationController_RegistrarException, registrar.GetType().Name, typeof(IWebHookRegistrar).Name, ex.Message);
                    _logger.LogError(msg);
                    throw ex;
                }
            }
        }


        /// <summary>
        /// Creates an <see cref="IActionResult"/> based on the provided <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The result to use when creating the <see cref="IActionResult"/>.</param>
        /// <returns>An initialized <see cref="IActionResult"/>.</returns>
        private IActionResult CreateResultFromStoreResult(StoreResult result)
        {
            switch (result)
            {
                case StoreResult.Success:
                    return Ok();

                case StoreResult.Conflict:
                    return new StatusCodeResult(StatusCodes.Status409Conflict);

                case StoreResult.NotFound:
                    return NotFound();

                case StoreResult.OperationError:
                    return BadRequest();

                default:
                    return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

    }
}
