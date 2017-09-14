using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks;
using Newtonsoft.Json.Linq;

namespace GitHubCoreReceiver.Controllers
{
    public class GitHubController : ControllerBase
    {
        [GitHubWebHookAction(EventName = "push")]
        public IActionResult HandlerForPush(string id, JObject data)
        {
            return Ok();
        }

        [GitHubWebHookAction(Id = "It")]
        public IActionResult HandlerForIt(string[] events, JObject data)
        {
            return Ok();
        }

        [GitHubWebHookAction]
        public IActionResult GitHubHandler(string id, string @event, JObject data)
        {
            return Ok();
        }

        [WebHookAction]
        public IActionResult FallbackHandler(string receiverName, string id, string eventName, JObject data)
        {
            return Ok();
        }
    }
}
