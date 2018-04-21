// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// Infrastructure supporting receiver-specific signature generators. Modeled after
    /// <c>WebHookVerifySignatureFilter</c> but intentionally separate. Separate to minimize dependencies in this test
    /// code, to provide a consistent baseline as <c>WebHookVerifySignatureFilter</c> changes, and to avoid major
    /// shifts in the <c>WebHookVerifySignatureFilter</c> API.
    /// </summary>
    public abstract class SignatureGenerator
    {
        /// <summary>
        /// Returns the SHA1 HMAC of the file named <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The name of the file to hash.</param>
        /// <param name="secretKey">The key data used to initialize the <see cref="HMACSHA1"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA1 HMAC of
        /// the <paramref name="filename"/>.
        /// </returns>
        protected static Task<byte[]> ComputeSha1HashAsync(string filename, string secretKey)
        {
            return ComputeSha1HashAsync(filename, secretKey, prefix: null);
        }

        /// <summary>
        /// Returns the SHA1 HMAC of the given <paramref name="prefix"/> followed by the file named
        /// <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The name of the file to hash.</param>
        /// <param name="secretKey">The key data used to initialize the <see cref="HMACSHA1"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>char</c>s to include in the hashed content
        /// before the file named <paramref name="filename"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA1 HMAC of
        /// the <paramref name="prefix"/> followed by the file named <paramref name="filename"/>.
        /// </returns>
        protected static Task<byte[]> ComputeSha1HashAsync(string filename, string secretKey, string prefix)
        {
            return ComputeSha1HashAsync(filename, secretKey, prefix, suffix: null);
        }

        /// <summary>
        /// Returns the SHA1 HMAC of the given <paramref name="prefix"/>, the file named <paramref name="filename"/>,
        /// and the given <paramref name="suffix"/> (in that order).
        /// </summary>
        /// <param name="filename">The name of the file to hash.</param>
        /// <param name="secretKey">The key data used to initialize the <see cref="HMACSHA1"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>char</c>s to include in the hashed content
        /// before the file named <paramref name="filename"/>.
        /// </param>
        /// <param name="suffix">
        /// If non-<see langword="null"/> and non-empty, additional <c>char</c>s to include in the hashed content
        /// after the file named <paramref name="filename"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA1 HMAC of
        /// the <paramref name="prefix"/>, the file named <paramref name="filename"/>, and the
        /// <paramref name="suffix"/> (in that order).
        /// </returns>
        protected static async Task<byte[]> ComputeSha1HashAsync(
            string filename,
            string secretKey,
            string prefix,
            string suffix)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }
            if (secretKey == null)
            {
                throw new ArgumentNullException(nameof(secretKey));
            }
            if (secretKey.Length == 0)
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(secretKey));
            }

            var secret = Encoding.UTF8.GetBytes(secretKey);
            using (var hasher = new HMACSHA1(secret))
            {
                if (prefix != null && prefix.Length > 0)
                {
                    var prefixBytes = Encoding.UTF8.GetBytes(prefix);
                    hasher.TransformBlock(
                        prefixBytes,
                        inputOffset: 0,
                        inputCount: prefix.Length,
                        outputBuffer: null,
                        outputOffset: 0);
                }

                // Split file into 4K chunks.
                var buffer = new byte[4096];
                using (var inputStream = new FileStream(
                    filename,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize: 4096,
                    useAsync: true))
                {
                    int bytesRead;
                    while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        hasher.TransformBlock(
                            buffer,
                            inputOffset: 0,
                            inputCount: bytesRead,
                            outputBuffer: null,
                            outputOffset: 0);
                    }
                }

                if (suffix != null && suffix.Length > 0)
                {
                    var suffixBytes = Encoding.UTF8.GetBytes(suffix);
                    hasher.TransformBlock(
                        suffixBytes,
                        inputOffset: 0,
                        inputCount: suffix.Length,
                        outputBuffer: null,
                        outputOffset: 0);
                }

                hasher.TransformFinalBlock(Array.Empty<byte>(), inputOffset: 0, inputCount: 0);

                return hasher.Hash;
            }
        }

        /// <summary>
        /// Returns the SHA256 HMAC of the file named <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The name of the file to hash.</param>
        /// <param name="secretKey">The key data used to initialize the <see cref="HMACSHA256"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA256 HMAC of
        /// the file named <paramref name="filename"/>.
        /// </returns>
        protected static Task<byte[]> ComputeSha256HashAsync(string filename, string secretKey)
        {
            return ComputeSha256HashAsync(filename, secretKey, prefix: null);
        }

        /// <summary>
        /// Returns the SHA256 HMAC of the given <paramref name="prefix"/> followed by the file named
        /// <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The name of the file to hash.</param>
        /// <param name="secretKey">The key data used to initialize the <see cref="HMACSHA256"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>char</c>s to include in the hashed content
        /// before the file named <paramref name="filename"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA256 HMAC of
        /// the <paramref name="prefix"/> followed by the file named <paramref name="filename"/>.
        /// </returns>
        protected static Task<byte[]> ComputeSha256HashAsync(string filename, string secretKey, string prefix)
        {
            return ComputeSha256HashAsync(filename, secretKey, prefix, suffix: null);
        }

        /// <summary>
        /// Returns the SHA256 HMAC of the given <paramref name="prefix"/>, the file named <paramref name="filename"/>,
        /// and the given <paramref name="suffix"/> (in that order).
        /// </summary>
        /// <param name="filename">The name of the file to hash.</param>
        /// <param name="secretKey">The key data used to initialize the <see cref="HMACSHA256"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>char</c>s to include in the hashed content
        /// before the file named <paramref name="filename"/>.
        /// </param>
        /// <param name="suffix">
        /// If non-<see langword="null"/> and non-empty, additional <c>char</c>s to include in the hashed content
        /// after the file named <paramref name="filename"/>.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA256 HMAC of
        /// the <paramref name="prefix"/>, the file named <paramref name="filename"/>, and the
        /// <paramref name="suffix"/> (in that order).
        /// </returns>
        protected static async Task<byte[]> ComputeSha256HashAsync(
            string filename,
            string secretKey,
            string prefix,
            string suffix)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }
            if (secretKey == null)
            {
                throw new ArgumentNullException(nameof(secretKey));
            }
            if (secretKey.Length == 0)
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(secretKey));
            }

            var secret = Encoding.UTF8.GetBytes(secretKey);
            using (var hasher = new HMACSHA256(secret))
            {
                if (prefix != null && prefix.Length > 0)
                {
                    var prefixBytes = Encoding.UTF8.GetBytes(prefix);
                    hasher.TransformBlock(
                        inputBuffer: prefixBytes,
                        inputOffset: 0,
                        inputCount: prefix.Length,
                        outputBuffer: null,
                        outputOffset: 0);
                }

                // Split content into 4K chunks.
                var buffer = new byte[4096];
                using (var inputStream = new FileStream(
                    filename,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    bufferSize: 4096,
                    useAsync: true))
                {
                    int bytesRead;
                    while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        hasher.TransformBlock(
                            buffer,
                            inputOffset: 0,
                            inputCount: bytesRead,
                            outputBuffer: null,
                            outputOffset: 0);
                    }
                }

                if (suffix != null && suffix.Length > 0)
                {
                    var suffixBytes = Encoding.UTF8.GetBytes(suffix);
                    hasher.TransformBlock(
                        suffixBytes,
                        inputOffset: 0,
                        inputCount: suffix.Length,
                        outputBuffer: null,
                        outputOffset: 0);
                }

                hasher.TransformFinalBlock(Array.Empty<byte>(), inputOffset: 0, inputCount: 0);

                return hasher.Hash;
            }
        }
    }
}
