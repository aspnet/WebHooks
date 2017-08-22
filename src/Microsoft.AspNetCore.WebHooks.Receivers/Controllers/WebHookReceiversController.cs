// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks.Routes;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Controllers
{
    /// <summary>
    /// Accepts incoming WebHook requests and dispatches them to registered <see cref="IWebHookReceiver"/> instances.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/webhooks/incoming")]
    public class WebHookReceiversController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IWebHookReceiverManager _receiverManager;

        public WebHookReceiversController(ILoggerFactory loggerFactory, IWebHookReceiverManager receiverManager)
        {
            _logger = loggerFactory.CreateLogger<WebHookReceiversController>();
            _receiverManager = receiverManager;
        }

        /// <summary>
        /// Supports GET for incoming WebHook request. This is typically used to verify that a WebHook is correctly wired up.
        /// </summary>
        [HttpGet("{webHookReceiver}/{id?}", Name = WebHookReceiverRouteNames.ReceiversAction)]
        [AllowAnonymous]
        [SuppressMessage("Microsoft.Design", "CA1026:Default parameters should not be used", Justification = "This is an established parameter binding pattern for Web API.")]
        public Task<IActionResult> Get(string webHookReceiver, string id = "")
        {
            return ProcessWebHook(webHookReceiver, id);
        }

        /// <summary>
        /// Supports HEAD for incoming WebHook request. This is typically used to verify that a WebHook is correctly wired up.
        /// </summary>
        [HttpHead("{webHookReceiver}/{id?}")]
        [AllowAnonymous]
        [SuppressMessage("Microsoft.Design", "CA1026:Default parameters should not be used", Justification = "This is an established parameter binding pattern for Web API.")]
        public Task<IActionResult> Head(string webHookReceiver, string id = "")
        {
            return ProcessWebHook(webHookReceiver, id);
        }

        /// <summary>
        /// Supports POST for incoming WebHook requests. This is typically the actual WebHook.
        /// </summary>
        [HttpPost("{webHookReceiver}/{id?}")]
        [AllowAnonymous]
        [SuppressMessage("Microsoft.Design", "CA1026:Default parameters should not be used", Justification = "This is an established parameter binding pattern for Web API.")]
        public Task<IActionResult> Post(string webHookReceiver, string id = "")
        {
            return ProcessWebHook(webHookReceiver, id);
        }

        private async Task<IActionResult> ProcessWebHook(string webHookReceiver, string id)
        {
            var receiver = _receiverManager.GetReceiver(webHookReceiver);
            if (receiver == null)
            {
                _logger.LogError(
                    0,
                    "No WebHook receiver is registered with the name '{WebHookReceiver}'.",
                    webHookReceiver);

                return NotFound();
            }

            _logger.LogInformation(
                1,
                "Processing incoming WebHook request with receiver '{WebHookReceiver}' and id '{Id}'.",
                webHookReceiver,
                id);

            var result = await receiver.ReceiveAsync(id, HttpContext, ModelState);
            if (result != null)
            {
                return result;
            }

            // Receiver should update ModelState before returning null...
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ... except if everything went perfectly.
            return Ok();
        }
    }
}