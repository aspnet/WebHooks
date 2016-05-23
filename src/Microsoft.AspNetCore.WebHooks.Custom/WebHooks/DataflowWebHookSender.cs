using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class DataflowWebHookSender : IWebHookSender
    {
        public Task SendWebHookWorkItemsAsync(IEnumerable<WebHookWorkItem> workItems)
        {
            throw new NotImplementedException();
        }
    }
}
