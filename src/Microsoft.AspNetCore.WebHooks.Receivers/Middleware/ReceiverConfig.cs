using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class ReceiverOptions
    {
        public string BasePath { get; set; }

        public ReceiverOptions()
        {
            this.BasePath = "/api/webhooks/incoming";
        }

    }
}
