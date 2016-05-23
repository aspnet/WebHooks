using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebHooks;
using Microsoft.AspNetCore.WebHooks.Receivers;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WebHookServiceCollectionExtensions
    {
        public static void AddCustomWebHookReceivers(this IServiceCollection services, Action<CustomWebHookReceiverOptions> setupAction)
        {
            CustomWebHookReceiverOptions options = new CustomWebHookReceiverOptions();
            services.Configure(setupAction);

            services.AddScoped<IWebHookReceiver, CustomWebHookReceiver>();
        }
    }
}
