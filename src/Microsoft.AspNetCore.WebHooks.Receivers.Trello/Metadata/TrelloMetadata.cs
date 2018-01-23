﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebHooks.Metadata
{
    /// <summary>
    /// An <see cref="IWebHookMetadata"/> service containing metadata about the Trello receiver.
    /// </summary>
    public class TrelloMetadata :
        WebHookMetadata,
        IWebHookBodyTypeMetadataService,
        IWebHookEventMetadata,
        IWebHookGetHeadRequestMetadata
    {
        /// <summary>
        /// Instantiates a new <see cref="TrelloMetadata"/> instance.
        /// </summary>
        public TrelloMetadata()
            : base(TrelloConstants.ReceiverName)
        {
        }

        // IWebHookBodyTypeMetadataService...

        /// <inheritdoc />
        public WebHookBodyType BodyType => WebHookBodyType.Json;

        // IWebHookEventMetadata...

        /// <inheritdoc />
        public string ConstantValue => TrelloConstants.EventName;

        /// <inheritdoc />
        public string HeaderName => null;

        /// <inheritdoc />
        public string QueryParameterName => null;

        // IWebHookGetHeadRequestMetadata...

        /// <inheritdoc />
        public bool AllowHeadRequests => true;

        /// <inheritdoc />
        public string ChallengeQueryParameterName => null;

        /// <inheritdoc />
        public int SecretKeyMinLength => TrelloConstants.SecretKeyMinLength;

        /// <inheritdoc />
        public int SecretKeyMaxLength => TrelloConstants.SecretKeyMaxLength;
    }
}
