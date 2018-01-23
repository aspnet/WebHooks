﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// Base class for <see cref="IWebHookReceiver"/> and <see cref="Mvc.Filters.IResourceFilter"/> or
    /// <see cref="Mvc.Filters.IAsyncResourceFilter"/> implementations that verify request body content e.g. filters
    /// that verify signatures of request body content. Subclasses should have an
    /// <see cref="Mvc.Filters.IOrderedFilter.Order"/> equal to <see cref="WebHookSecurityFilter.Order"/>.
    /// </summary>
    public abstract class WebHookVerifySignatureFilter : WebHookSecurityFilter, IWebHookReceiver
    {
        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifySignatureFilter"/> instance.
        /// </summary>
        /// <param name="configuration">
        /// The <see cref="IConfiguration"/> used to initialize <see cref="WebHookSecurityFilter.Configuration"/>.
        /// </param>
        /// <param name="hostingEnvironment">
        /// The <see cref="IHostingEnvironment" /> used to initialize
        /// <see cref="WebHookSecurityFilter.HostingEnvironment"/>.
        /// </param>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="WebHookSecurityFilter.Logger"/>.
        /// </param>
        protected WebHookVerifySignatureFilter(
            IConfiguration configuration,
            IHostingEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory)
            : base(configuration, hostingEnvironment, loggerFactory)
        {
        }

        /// <inheritdoc />
        public abstract string ReceiverName { get; }

        /// <inheritdoc />
        public bool IsApplicable(string receiverName)
        {
            if (receiverName == null)
            {
                throw new ArgumentNullException(nameof(receiverName));
            }

            return string.Equals(ReceiverName, receiverName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Converts a base64-encoded string to a <see cref="T:byte[]"/> (with or without URI-safe mode encoding).
        /// </summary>
        /// <param name="content">The Base64-encoded string to convert.</param>
        /// <returns>The converted <see cref="T:byte[]"/>.</returns>
        public static byte[] FromBase64(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return new byte[0];
            }

            // Reverse URI-safe encoding.
            var base64 = content
                .Replace('_', '/')
                .Replace('-', '+');

            // Append padding if missing in content.
            switch (content.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;

                case 3:
                    base64 += "=";
                    break;
            }

            // May throw a FormatException if base64 is not valid.
            var data = Convert.FromBase64String(base64);

            return data;
        }

        /// <summary>
        /// Converts a hex-encoded string to a <see cref="T:byte[]"/>.
        /// </summary>
        /// <param name="content">THe hex-encoded string to convert.</param>
        /// <returns>The converted <see cref="T:byte[]"/>.</returns>
        protected static byte[] FromHex(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return Array.Empty<byte>();
            }

            byte[] data = null;
            try
            {
                data = new byte[content.Length / 2];
                var input = 0;
                for (var output = 0; output < data.Length; output++)
                {
                    data[output] = Convert.ToByte(new string(new char[2] { content[input++], content[input++] }), 16);
                }

                if (input != content.Length)
                {
                    data = null;
                }
            }
            catch
            {
                data = null;
            }

            if (data == null)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifySignature_InvalidHexValue,
                    content);
                throw new InvalidOperationException(message);
            }

            return data;
        }

        /// <summary>
        /// Provides a time consistent comparison of two secrets in the form of two byte arrays.
        /// </summary>
        /// <param name="inputA">The first secret to compare.</param>
        /// <param name="inputB">The second secret to compare.</param>
        /// <returns><see langword="true"/> if the two secrets are equal; <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        protected static bool SecretEqual(byte[] inputA, byte[] inputB)
        {
            if (ReferenceEquals(inputA, inputB))
            {
                return true;
            }

            if (inputA == null || inputB == null || inputA.Length != inputB.Length)
            {
                return false;
            }

            var areSame = true;
            for (var i = 0; i < inputA.Length; i++)
            {
                areSame &= inputA[i] == inputB[i];
            }

            return areSame;
        }

        /// <summary>
        /// Gets the value of a given HTTP request <paramref name="headerName"/>. If the field is either not present in
        /// the <paramref name="request"/> or has more than one value then an error is generated and returned in
        /// <paramref name="errorResult"/>.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="headerName">The name of the HTTP request header to look up.</param>
        /// <param name="errorResult">
        /// Set to <see langword="null"/> in the success case. When a check fails, an <see cref="IActionResult"/> that
        /// when executed will produce a response containing details about the problem.
        /// </param>
        /// <returns>The signature header; <see langword="null"/> if this cannot be found.</returns>
        protected virtual string GetRequestHeader(
            HttpRequest request,
            string headerName,
            out IActionResult errorResult)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            if (!request.Headers.TryGetValue(headerName, out var headers) || headers.Count != 1)
            {
                var headersCount = headers.Count;
                Logger.LogInformation(
                    400,
                    "Expecting exactly one '{HeaderName}' header field in the WebHook request but found " +
                    "{HeaderCount}. Please ensure the request contains exactly one '{HeaderName}' header field.",
                    headerName,
                    headersCount,
                    headerName);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.VerifySignature_BadHeader,
                    headerName,
                    headersCount);
                errorResult = new BadRequestObjectResult(message);

                return null;
            }

            errorResult = null;

            return headers;
        }

        /// <summary>
        /// Returns the SHA1 HMAC of the given <paramref name="request"/>'s body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="secret">The key data used to initialize the <see cref="HMACSHA1"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA1 HMAC of
        /// the <paramref name="request"/>'s body.
        /// </returns>
        protected Task<byte[]> ComputeRequestBodySha1HashAsync(HttpRequest request, byte[] secret)
        {
            return ComputeRequestBodySha1HashAsync(request, secret, prefix: null);
        }

        /// <summary>
        /// Returns the SHA1 HMAC of the given <paramref name="prefix"/> followed by the given
        /// <paramref name="request"/>'s body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="secret">The key data used to initialize the <see cref="HMACSHA1"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>byte</c>s to include in the hashed content
        /// before the <paramref name="request"/>'s body.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA1 HMAC of
        /// the <paramref name="prefix"/> followed by the <paramref name="request"/>'s body.
        /// </returns>
        protected Task<byte[]> ComputeRequestBodySha1HashAsync(HttpRequest request, byte[] secret, byte[] prefix)
        {
            return ComputeRequestBodySha1HashAsync(request, secret, prefix, suffix: null);
        }

        /// <summary>
        /// Returns the SHA1 HMAC of the given <paramref name="prefix"/>, the given <paramref name="request"/>'s
        /// body, and the given <paramref name="suffix"/> (in that order).
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="secret">The key data used to initialize the <see cref="HMACSHA1"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>byte</c>s to include in the hashed content
        /// before the <paramref name="request"/>'s body.
        /// </param>
        /// <param name="suffix">
        /// If non-<see langword="null"/> and non-empty, additional <c>byte</c>s to include in the hashed content
        /// after the <paramref name="request"/>'s body.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA1 HMAC of
        /// the <paramref name="prefix"/>, the <paramref name="request"/>'s body, and the <paramref name="suffix"/>
        /// (in that order).
        /// </returns>
        protected virtual async Task<byte[]> ComputeRequestBodySha1HashAsync(
            HttpRequest request,
            byte[] secret,
            byte[] prefix,
            byte[] suffix)
        {
            await WebHookHttpRequestUtilities.PrepareRequestBody(request);

            using (var hasher = new HMACSHA1(secret))
            {
                try
                {
                    if (prefix != null && prefix.Length > 0)
                    {
                        hasher.TransformBlock(
                            prefix,
                            inputOffset: 0,
                            inputCount: prefix.Length,
                            outputBuffer: null,
                            outputOffset: 0);
                    }

                    // Split body into 4K chunks.
                    var buffer = new byte[4096];
                    var inputStream = request.Body;
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

                    if (suffix != null && suffix.Length > 0)
                    {
                        hasher.TransformBlock(
                            suffix,
                            inputOffset: 0,
                            inputCount: suffix.Length,
                            outputBuffer: null,
                            outputOffset: 0);
                    }

                    hasher.TransformFinalBlock(Array.Empty<byte>(), inputOffset: 0, inputCount: 0);

                    return hasher.Hash;
                }
                finally
                {
                    // Reset Position because JsonInputFormatter et cetera always start from current position.
                    request.Body.Seek(0L, SeekOrigin.Begin);
                }
            }
        }

        /// <summary>
        /// Returns the SHA256 HMAC of the given <paramref name="request"/>'s body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="secret">The key data used to initialize the <see cref="HMACSHA256"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA256 HMAC of
        /// the <paramref name="request"/>'s body.
        /// </returns>
        protected Task<byte[]> ComputeRequestBodySha256HashAsync(HttpRequest request, byte[] secret)
        {
            return ComputeRequestBodySha256HashAsync(request, secret, prefix: null);
        }

        /// <summary>
        /// Returns the SHA256 HMAC of the given <paramref name="prefix"/> followed by the given
        /// <paramref name="request"/>'s body.
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="secret">The key data used to initialize the <see cref="HMACSHA256"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>byte</c>s to include in the hashed content
        /// before the <paramref name="request"/>'s body.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA256 HMAC of
        /// the <paramref name="prefix"/> followed by the <paramref name="request"/>'s body.
        /// </returns>
        protected Task<byte[]> ComputeRequestBodySha256HashAsync(HttpRequest request, byte[] secret, byte[] prefix)
        {
            return ComputeRequestBodySha256HashAsync(request, secret, prefix, suffix: null);
        }

        /// <summary>
        /// Returns the SHA256 HMAC of the given <paramref name="prefix"/>, the given <paramref name="request"/>'s
        /// body, and the given <paramref name="suffix"/> (in that order).
        /// </summary>
        /// <param name="request">The current <see cref="HttpRequest"/>.</param>
        /// <param name="secret">The key data used to initialize the <see cref="HMACSHA256"/>.</param>
        /// <param name="prefix">
        /// If non-<see langword="null"/> and non-empty, additional <c>byte</c>s to include in the hashed content
        /// before the <paramref name="request"/>'s body.
        /// </param>
        /// <param name="suffix">
        /// If non-<see langword="null"/> and non-empty, additional <c>byte</c>s to include in the hashed content
        /// after the <paramref name="request"/>'s body.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that on completion provides a <see cref="byte"/> array containing the SHA256 HMAC of
        /// the <paramref name="prefix"/>, the <paramref name="request"/>'s body, and the <paramref name="suffix"/>
        /// (in that order).
        /// </returns>
        protected virtual async Task<byte[]> ComputeRequestBodySha256HashAsync(
            HttpRequest request,
            byte[] secret,
            byte[] prefix,
            byte[] suffix)
        {
            await WebHookHttpRequestUtilities.PrepareRequestBody(request);

            using (var hasher = new HMACSHA256(secret))
            {
                try
                {
                    if (prefix != null && prefix.Length > 0)
                    {
                        hasher.TransformBlock(
                            inputBuffer: prefix,
                            inputOffset: 0,
                            inputCount: prefix.Length,
                            outputBuffer: null,
                            outputOffset: 0);
                    }

                    // Split body into 4K chunks.
                    var buffer = new byte[4096];
                    var inputStream = request.Body;
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

                    if (suffix != null && suffix.Length > 0)
                    {
                        hasher.TransformBlock(
                            suffix,
                            inputOffset: 0,
                            inputCount: suffix.Length,
                            outputBuffer: null,
                            outputOffset: 0);
                    }

                    hasher.TransformFinalBlock(Array.Empty<byte>(), inputOffset: 0, inputCount: 0);

                    return hasher.Hash;
                }
                finally
                {
                    // Reset Position because JsonInputFormatter et cetera always start from current position.
                    request.Body.Seek(0L, SeekOrigin.Begin);
                }
            }
        }

        /// <summary>
        /// Decode the given <paramref name="hexEncodedValue"/>.
        /// </summary>
        /// <param name="hexEncodedValue">The hex-encoded <see cref="string"/>.</param>
        /// <param name="signatureHeaderName">
        /// The name of the HTTP header containing the <paramref name="hexEncodedValue"/>.
        /// </param>
        /// <param name="errorResult">
        /// Set to <see langword="null"/> if decoding is successful. Otherwise, an <see cref="IActionResult"/> that
        /// when executed will produce a response containing details about the problem.
        /// </param>
        /// <returns>
        /// If successful, the <see cref="byte"/> array containing the decoded hash. <see langword="null"/> if any
        /// issues occur.
        /// </returns>
        protected virtual byte[] GetDecodedHash(
            string hexEncodedValue,
            string signatureHeaderName,
            out IActionResult errorResult)
        {
            try
            {
                var decodedHash = FromHex(hexEncodedValue);
                errorResult = null;

                return decodedHash;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    401,
                    ex,
                    "The '{HeaderName}' header value is invalid. It must be a valid hex-encoded string.",
                    signatureHeaderName);
            }

            var message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.VerifySignature_BadHeaderEncoding,
                signatureHeaderName);
            errorResult = new BadRequestObjectResult(message);

            return null;
        }

        /// <summary>
        /// Decode the given <paramref name="base64EncodedValue"/>.
        /// </summary>
        /// <param name="base64EncodedValue">The Base64-encoded <see cref="string"/>.</param>
        /// <param name="signatureHeaderName">
        /// The name of the HTTP header containing the <paramref name="base64EncodedValue"/>.
        /// </param>
        /// <param name="errorResult">
        /// Set to <see langword="null"/> if decoding is successful. Otherwise, an <see cref="IActionResult"/> that
        /// when executed will produce a response containing details about the problem.
        /// </param>
        /// <returns>
        /// If successful, the <see cref="byte"/> array containing the decoded hash. <see langword="null"/> if any
        /// issues occur.
        /// </returns>
        protected virtual byte[] GetBase64DecodedHash(
            string base64EncodedValue,
            string signatureHeaderName,
            out IActionResult errorResult)
        {
            try
            {
                var decodedHash = FromBase64(base64EncodedValue);
                errorResult = null;

                return decodedHash;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    401,
                    ex,
                    "The '{HeaderName}' header value is invalid. It must be a valid Base64-encoded string.",
                    signatureHeaderName);
            }

            var message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.VerifySignature_BadBase64HeaderEncoding,
                signatureHeaderName);
            errorResult = new BadRequestObjectResult(message);

            return null;
        }

        /// <summary>
        /// Returns a new <see cref="IActionResult"/> that when executed produces a response indicating that a
        /// request had an invalid signature and as a result could not be processed.
        /// </summary>
        /// <param name="receiverName">The name of an available <see cref="IWebHookReceiver"/>.</param>
        /// <param name="signatureHeaderName">The name of the HTTP header with invalid contents.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> that when executed will produce a response with status code 400 "Bad
        /// Request" and containing details about the problem.
        /// </returns>
        protected virtual IActionResult CreateBadSignatureResult(string receiverName, string signatureHeaderName)
        {
            Logger.LogError(
                402,
                "The WebHook signature provided by the '{HeaderName}' header field does not match the value " +
                "expected by the '{ReceiverName}' receiver. WebHook request is invalid.",
                signatureHeaderName,
                receiverName);

            var message = string.Format(
                CultureInfo.CurrentCulture,
                Resources.VerifySignature_BadSignature,
                signatureHeaderName,
                receiverName);
            var badSignature = new BadRequestObjectResult(message);

            return badSignature;
        }
    }
}
