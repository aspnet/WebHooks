using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks.Routes;
using Microsoft.AspNetCore.WebHooks.WebHooks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Controllers
{
    /// <summary>
    /// The <see cref="WebHookRegistrationsController"/> allows the caller to create, modify, and manage WebHooks
    /// through a REST-style interface.
    /// </summary>
    [Authorize]
    [Route("api/webhooks/registrations")]
    public class WebHookRegistrationsController : ControllerBase
    {
        private readonly IWebHookRegistrationsManager _registrationsManager;
        private readonly IWebHookIdValidator _idValidator;
        private readonly IEnumerable<IWebHookRegistrar> _webHookRegistrars;
        private readonly ILogger _logger;

        /// <inheritdoc />
        public WebHookRegistrationsController(IWebHookRegistrationsManager registrationsManager,
            IWebHookIdValidator idValidator,
            IEnumerable<IWebHookRegistrar> webHookRegistrars,
            ILogger<WebHookRegistrationsController> logger)
        {
            _registrationsManager = registrationsManager;
            _idValidator = idValidator;
            _webHookRegistrars = webHookRegistrars;
            _logger = logger;
        }

        /// <summary>
        /// Gets all registered WebHooks for a given user.
        /// </summary>
        /// <returns>A collection containing the registered <see cref="WebHook"/> instances for a given user.</returns>
        [Route("")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var webHooks = await _registrationsManager.GetWebHooksAsync(User, RemovePrivateFilters);
                return Ok(webHooks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Looks up a registered WebHook with the given <paramref name="id"/> for a given user.
        /// </summary>
        /// <returns>The registered <see cref="WebHook"/> instance for a given user.</returns>
        [Route("{id}", Name = WebHookRouteNames.RegistrationLookupAction)]
        [HttpGet]
        public async Task<IActionResult> Lookup(string id)
        {
            try
            {
                var webHook = await _registrationsManager.LookupWebHookAsync(User, id, RemovePrivateFilters);
                if (webHook != null)
                {
                    return Ok(webHook);
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Registers a new WebHook for a given user.
        /// </summary>
        /// <param name="webHook">The <see cref="WebHook"/> to create.</param>
        [Route("")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]WebHook webHook)
        {
            if (webHook == null)
            {
                return BadRequest();
            }

            try
            {
                // Validate the provided WebHook ID (or force one to be created on server side)
                await _idValidator.ValidateIdAsync(Request, webHook);

                // Validate other parts of WebHook
                await _registrationsManager.VerifySecretAsync(webHook);
                await _registrationsManager.VerifyFiltersAsync(webHook);
                await _registrationsManager.VerifyAddressAsync(webHook);
            }
            catch (Exception ex)
            {
                var message = $"Could not register WebHook due to error: {ex.Message}";
                _logger.LogInformation(ex.Message, ex);
                return BadRequest(message);
            }

            try
            {
                // Add WebHook for this user.
                var result = await _registrationsManager.AddWebHookAsync(User, webHook, AddPrivateFilters);
                if (result == StoreResult.Success)
                {
                    return CreatedAtRoute(WebHookRouteNames.RegistrationLookupAction, new { id = webHook.Id }, webHook);
                }
                return CreateHttpResult(result);
            }
            catch (Exception ex)
            {
                var message = $"Could not register WebHook due to error: {ex.Message}";
                _logger.LogInformation(ex.Message, ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, message);
            }
        }

        /// <summary>
        /// Updates an existing WebHook registration.
        /// </summary>
        /// <param name="id">The WebHook ID.</param>
        /// <param name="webHook">The new <see cref="WebHook"/> to use.</param>
        [Route("{id}")]
        [HttpPatch]
        public async Task<IActionResult> Put(string id, WebHook webHook)
        {
            if (webHook == null)
            {
                return BadRequest();
            }
            if (!string.Equals(id, webHook.Id, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            try
            {
                // Validate parts of WebHook
                await _registrationsManager.VerifySecretAsync(webHook);
                await _registrationsManager.VerifyFiltersAsync(webHook);
                await _registrationsManager.VerifyAddressAsync(webHook);
            }
            catch (Exception ex)
            {
                var message = $"Could not register WebHook due to error: {ex.Message}";
                _logger.LogInformation(ex.Message, ex);
                return BadRequest(message);
            }

            try
            {
                // Update WebHook for this user  
                var result = await _registrationsManager.UpdateWebHookAsync(User, webHook, AddPrivateFilters);
                return CreateHttpResult(result);
            }
            catch (Exception ex)
            {
                var message = $"Could not register WebHook due to error: {ex.Message}";
                _logger.LogInformation(ex.Message, ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, message);
            }
        }

        /// <summary>
        /// Deletes an existing WebHook registration.
        /// </summary>
        /// <param name="id">The WebHook ID.</param>
        [Route("{id}")]
        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _registrationsManager.DeleteWebHookAsync(User, id);
                return CreateHttpResult(result);
            }
            catch (Exception ex)
            {
                var message = $"Could not delete WebHook due to error: {ex.Message}";
                _logger.LogInformation(ex.Message, ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, message);
            }
        }

        /// <summary>
        /// Deletes all existing WebHook registrations.
        /// </summary>
        [Route("")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _registrationsManager.DeleteAllWebHooksAsync(User);
                return Ok();
            }
            catch (Exception ex)
            {
                var message = $"Could not delete WebHooks due to error: {ex.Message}";
                _logger.LogInformation(ex.Message, ex);
                return StatusCode((int)HttpStatusCode.InternalServerError, message);
            }
        }

        /// <summary>
        /// Removes all private (server side) filters from the given <paramref name="webHook"/>.
        /// </summary>
        protected virtual Task RemovePrivateFilters(string user, WebHook webHook)
        {
            if (webHook == null)
            {
                throw new ArgumentNullException(nameof(webHook));
            }

            var filters = webHook.Filters.Where(f => f.StartsWith(WebHookRegistrar.PrivateFilterPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
            foreach (var filter in filters)
            {
                webHook.Filters.Remove(filter);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// Executes all <see cref="IWebHookRegistrar"/> instances for server side manipulation, inspection, or
        /// rejection of registrations. This can for example be used to add server side only filters that
        /// are not governed by <see cref="IWebHookFilterManager"/>.
        /// </summary>
        protected virtual async Task<IActionResult> AddPrivateFilters(string user, WebHook webHook)
        {
            foreach (var registrar in _webHookRegistrars)
            {
                try
                {
                    await registrar.RegisterAsync(Request, webHook);
                }
                catch (Exception ex)
                {
                    var message = $"The '{registrar.GetType().Name}' implementation of '{typeof(IWebHookRegistrar).Name}' caused an exception: {ex.Message}";
                    return BadRequest(message);
                }
            }

            return Ok();
        }

        /// <summary>
        /// Creates an <see cref="IActionResult"/> based on the provided <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The result to use when creating the <see cref="IActionResult"/>.</param>
        /// <returns>An initialized <see cref="IActionResult"/>.</returns>
        private IActionResult CreateHttpResult(StoreResult result)
        {
            switch (result)
            {
                case StoreResult.Success:
                    return Ok();

                case StoreResult.Conflict:
                    return StatusCode((int) HttpStatusCode.Conflict);
                case StoreResult.NotFound:
                    return NotFound();

                case StoreResult.OperationError:
                    return BadRequest();

                default:
                    return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
}
