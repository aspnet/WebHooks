using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks;

namespace AzureAlertCoreReceiver.Controllers
{
    public class KuduController : ControllerBase
    {
        [KuduWebHook(Id = "It")]
        public IActionResult AzureAlertForIt(KuduNotification data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the notification message
            var message = data.Message;

            // Get the notification author
            var author = data.Author;

            return Ok();
        }

        [KuduWebHook]
        public IActionResult AzureAlert(string id, KuduNotification data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the notification message
            var message = data.Message;

            // Get the notification author
            var author = data.Author;

            return Ok();
        }
    }
}