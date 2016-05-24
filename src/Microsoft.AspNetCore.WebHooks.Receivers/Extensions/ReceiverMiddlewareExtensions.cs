using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebHooks;

namespace Microsoft.AspNetCore.Builder
{

    /// <summary>
    /// Provides an Extension to the <see cref="IApplicationBuilder"/>
    /// </summary>
    public static class ReceiverMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebHookReceivers(this IApplicationBuilder builder, Action<ReceiverOptions> setupAction)
        {
            var recConfig = new ReceiverOptions();
            setupAction(recConfig);

            return builder.Map(recConfig.BasePath, config => config.UseMiddleware<ReceiverMiddleware>());
        }

        public static IApplicationBuilder UseWebHookReceivers(this IApplicationBuilder builder)
        {
            return UseWebHookReceivers(builder, setup => { });
        }
    }
}
