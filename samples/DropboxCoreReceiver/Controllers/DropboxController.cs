using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace AzureAlertCoreReceiver.Controllers
{
    public class DropboxController : ControllerBase
    {
        [DropboxWebHook(Id = "It")]
        public IActionResult AzureAlertForIt(JObject data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok();
        }

        [DropboxWebHook]
        public IActionResult AzureAlert(string id, JObject data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            return Ok();
        }
    }
}