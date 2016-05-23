using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks;
using Microsoft.AspNetCore.Authorization;

namespace CoreCustomSender.Controllers
{
    [Produces("application/json")]
    [Route("api/Notify")]
    [Authorize]
    public class NotifyController : Controller
    {

        [HttpPost("Submit")]
        public async Task<ActionResult> Submit()
        {
            try
            {
                // Create an event with action 'event1' and additional data
                await this.NotifyAsync("event1", new { P1 = "p1" });

            }
             catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            return new EmptyResult();
        }

        [HttpPost("Post")]
        public async Task<ActionResult> Post()
        {
            // Create an event with 'event2' and additional data
            await this.NotifyAsync("event2", new { P1 = "p1" });
            return Ok();
        }
    }
}