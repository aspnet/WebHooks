// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.WebHooks.Extensions
{
    public static class WebHookApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseWebHooks(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Handle index.html files that user projects often include.
            builder.UseStaticFiles();
            builder.UseDefaultFiles();

            // Few MVC requirements beyond attribute routing.
            builder.UseMvc();

            return builder;
        }
    }
}
