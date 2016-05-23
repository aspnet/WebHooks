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
        /// <summary>
        /// Extension to Register Custom WebHook Senders
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services with</param>
        /// <param name="setupAction">A <see cref="Action{WebHookOptions}"/> that can be used to configure Custom WebHook Sender</param>
        public static void AddCustomWebHookSender(this IServiceCollection services, Action<WebHookOptions> setupAction)
        {
            WebHookOptions options = new WebHookOptions();
            setupAction(options);

            services.AddScoped(typeof(IWebHookStore), options.StoreType);
            services.AddScoped(typeof(IWebHookSender), options.SenderType);
            services.AddScoped<IWebHookManager, WebHookManager>();


        }

        /// <summary>
        /// Extension to Register Custom WebHook Senders with Default of Memory Store, and Data Flow Sender
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to register services with</param>
        public static void AddCustomWebHookSender(this IServiceCollection services)
        {
            services.AddCustomWebHookSender(opts => { });
        }
    }
}
