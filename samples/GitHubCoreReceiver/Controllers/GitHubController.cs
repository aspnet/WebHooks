using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks;
using Newtonsoft.Json.Linq;

namespace GitHubCoreReceiver.Controllers
{
    public class GitHubController : ControllerBase
    {
        [GitHubWebHook(EventName = "push")]
        public IActionResult HandlerForPush(string id, JObject data)
        {
            return Ok();
        }

        [GitHubWebHook(Id = "It")]
        public IActionResult HandlerForIt(string[] events, JObject data)
        {
            return Ok();
        }

        [GitHubWebHook]
        public IActionResult GitHubHandler(string id, string @event, JObject data)
        {
            return Ok();
        }

        [GeneralWebHook]
        public IActionResult FallbackHandler(string receiverName, string id, string eventName, JObject data)
        {
            return Ok();
        }
    }
}
