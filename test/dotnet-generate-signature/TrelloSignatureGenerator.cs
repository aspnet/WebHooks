// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class TrelloSignatureGenerator : SignatureGenerator
    {
        // Value used by default in TrelloCoreReceiver.
        public static readonly string DefaultSecretKey = "01234567890123456789012345678901";

        public static readonly string DefaultUrl = "https://localhost/api/webhooks/incoming/trello";

        public static async Task<string> Compute(string filename, string secretKey, string url)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var bytes = await ComputeSha1HashAsync(
                filename,
                secretKey ?? DefaultSecretKey,
                prefix: null,
                suffix: url ?? DefaultUrl);
            var hash = Convert.ToBase64String(bytes, Base64FormattingOptions.None);

            return hash;
        }
    }
}
