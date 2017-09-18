// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    public class KuduMetadata : WebHookMetadata, IWebHookRequestMetadataService, IWebHookSecurityMetadata
    {
        public KuduMetadata()
            : base(KuduConstants.ReceiverName)
        {
        }

        /// <inheritdoc />
        public WebHookBodyType BodyType => WebHookBodyType.Json;

        /// <inheritdoc />
        public bool VerifyCodeParameter => true;
    }
}
