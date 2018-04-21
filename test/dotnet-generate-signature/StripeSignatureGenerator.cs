// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class StripeSignatureGenerator : SignatureGenerator
    {
        // Value used by default in StripeCoreReceiver.
        public static readonly string DefaultSecretKey = "0123456789012345";

        // 21042018T13:53:39-07:00 aka Saturday, April 21, 2018 1:53:39 PM PDT
        public static readonly string DefaultTimestamp = "1524344019.";

        public static async Task<string> Compute(string filename, string secretKey, string timestamp)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            if (timestamp == null)
            {
                timestamp = DefaultTimestamp;
            }
            else if (!timestamp.EndsWith(".", StringComparison.Ordinal))
            {
                timestamp = timestamp + ".";
            }

            var bytes = await ComputeSha256HashAsync(filename, secretKey ?? DefaultSecretKey, prefix: timestamp);
            var hash = BitConverter.ToString(bytes).Replace("-", string.Empty);

            return hash;
        }
    }
}
