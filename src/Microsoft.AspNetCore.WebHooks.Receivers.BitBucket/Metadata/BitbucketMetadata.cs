using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    public class BitbucketMetadata :
        WebHookMetadata,
        IWebHookEventMetadata,
        IWebHookRequestMetadataService,
        IWebHookSecurityMetadata
    {
        public BitbucketMetadata()
            : base(BitbucketConstants.ReceiverName)
        {
        }

        // IWebHookBindingMetadata...

        /// <inheritdoc />
        public IReadOnlyList<WebHookParameter> Parameters { get; } = new List<WebHookParameter>
        {
            new WebHookParameter(
                BitbucketConstants.WebHookIdParameterName1,
                BitbucketConstants.WebHookIdHeaderName,
                isQueryParameter: false,
                isRequired: true),
            new WebHookParameter(
                BitbucketConstants.WebHookIdParameterName2,
                BitbucketConstants.WebHookIdHeaderName,
                isQueryParameter: false,
                isRequired: true),
        };

        // IWebHookEventMetadata...

        /// <inheritdoc />
        public string ConstantValue => null;

        public string HeaderName => BitbucketConstants.EventHeaderName;

        public string PingEventName => null;

        public string QueryParameterKey => null;

        public WebHookBodyType BodyType => WebHookBodyType.Json;

        public bool VerifyCodeParameter => true;
    }
}
