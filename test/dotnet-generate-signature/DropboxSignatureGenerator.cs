// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    public class DropboxSignatureGenerator : SignatureGenerator
    {
        // Value used by default in DropboxCoreReceiver.
        public static readonly string DefaultSecretKey = "012345678901234";

        public static async Task<string> Compute(string filename, string secretKey)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            var bytes = await ComputeSha256HashAsync(filename, secretKey ?? DefaultSecretKey);
            var hash = BitConverter.ToString(bytes).Replace("-", string.Empty);

            return hash;
        }
    }
}
