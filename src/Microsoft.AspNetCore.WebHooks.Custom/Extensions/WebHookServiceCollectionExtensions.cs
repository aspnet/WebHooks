using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebHooks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class WebHookServiceCollectionExtensions
    {
        public static void AddCustomWebHooks(this IServiceCollection services, Action<WebHookOptions> setupAction)
        {
            WebHookOptions options = new WebHookOptions();
            setupAction(options);

            services.AddScoped(typeof(IWebHookStore), options.StoreType);
            services.AddScoped(typeof(IWebHookSender), options.SenderType);
            services.AddScoped<IWebHookManager, WebHookManager>();


        }

        public static void AddCustomWebHooks(this IServiceCollection services)
        {
            services.AddCustomWebHooks(opts => { });
        }
    }
}
