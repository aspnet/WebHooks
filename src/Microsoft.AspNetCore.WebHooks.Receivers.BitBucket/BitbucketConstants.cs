using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.WebHooks
{
    public static class BitbucketConstants
    {
        public static string ReceiverName => "bitbucket";

        public static string EventHeaderName => "X-Event-Key";

        public static string WebHookIdHeaderName => "X-Hook-UUID";

        public static string WebHookIdRouteKeyName => "webHook_id";
    }
}
