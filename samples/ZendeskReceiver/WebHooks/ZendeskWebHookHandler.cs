using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.WebHooks;
using Newtonsoft.Json.Linq;

namespace ZendeskReceiver.WebHooks
{
    public class ZendeskWebHookHandler : WebHookHandler
    {
        public ZendeskWebHookHandler()
        {
            Receiver = "zendesk";
        }

        public override Task ExecuteAsync(string generator, WebHookHandlerContext context)
        {
            // For more information about Zendesk push payloads, please see 
            // 'https://developer.zendesk.com/embeddables/docs/android/push_notifications_webhook#notification-payload-handling'
            JObject entry = context.GetDataOrDefault<JObject>();

            // Extract the action -- for Zendesk we have only one.
            string action = context.Actions.First();
            
			//Implementation logic goes here

            return Task.FromResult(true);
        }
    }
}