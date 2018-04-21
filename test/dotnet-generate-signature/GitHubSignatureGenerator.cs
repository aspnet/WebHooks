// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class GitHubSignatureGenerator : SignatureGenerator
    {
        // Value used by default in GitHubCoreReceiver.
        public static readonly string DefaultSecretKey = "0123456789012345";

        public static async Task<string> Compute(string filename, string secretKey)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var bytes = await ComputeSha1HashAsync(filename, secretKey ?? DefaultSecretKey);
            var hash = BitConverter.ToString(bytes).Replace("-", string.Empty);

            return hash;
        }
    }
}
