// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Utilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> that verifies the GitHub signature header. Confirms the header exists, reads
    /// Body bytes, and compares the hashes.
    /// </summary>
    public class GitHubWebHookVerifySignatureFilter : WebHookReceiver, IAsyncResourceFilter
    {
        internal const int SecretMinLength = 16;
        internal const int SecretMaxLength = 128;

        internal const string SignatureHeaderKey = "sha1";
        internal const string SignatureHeaderValueTemplate = SignatureHeaderKey + "={0}";
        internal const string SignatureHeaderName = "X-Hub-Signature";

        /// <summary>
        /// Instantiates a new <see cref="GitHubWebHookVerifySignatureFilter"/> instance.
        /// </summary>
        /// <param name="loggerFactory">
        /// The <see cref="ILoggerFactory"/> used to initialize <see cref="Logger"/>.
        /// </param>
        /// <param name="receiverConfig">The <see cref="IWebHookReceiverConfig"/>.</param>
        public GitHubWebHookVerifySignatureFilter(ILoggerFactory loggerFactory, IWebHookReceiverConfig receiverConfig)
            : base(loggerFactory, receiverConfig)
        {
        }

        /// <inheritdoc />
        public override string ReceiverName => GitHubWebHookConstants.ReceiverName;

        /// <inheritdoc />
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (context.RouteData.TryGetReceiverName(out var receiver) && IsApplicable(receiver))
            {
                // 1. Get the expected hash from the signature header.
                var request = context.HttpContext.Request;
                var header = GetRequestHeader(request, SignatureHeaderName, out var error);
                if (error != null)
                {
                    context.Result = error;
                    return;
                }

                // ??? Do we have efficient name / value parsers somewhere in HttpAbstractions?
                var values = header.SplitAndTrim('=');
                if (values.Length != 2 ||
                    !string.Equals(values[0], SignatureHeaderKey, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogError(
                        1,
                        "Invalid '{HeaderName}' header value. Expecting a value of '{Key}={Value}'.",
                        SignatureHeaderName,
                        SignatureHeaderKey,
                        "<value>");

                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Receiver_BadHeaderValue,
                        SignatureHeaderName,
                        SignatureHeaderKey,
                        "<value>");
                    var invalidHeader = WebHookResultUtilities.CreateErrorResult(message);

                    context.Result = invalidHeader;
                    return;
                }

                // TODO: If other FromHex() calls deal with headers, move into Receivers project.
                byte[] expectedHash;
                try
                {
                    expectedHash = EncodingUtilities.FromHex(values[1]);
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        1,
                        ex,
                        "The '{HeaderName}' header value is invalid. It must be a valid hex-encoded string.",
                        SignatureHeaderName);

                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Receiver_BadHeaderEncoding,
                        SignatureHeaderName);
                    var invalidEncoding = WebHookResultUtilities.CreateErrorResult(message);

                    context.Result = invalidEncoding;
                    return;
                }

                // 2. Get the configured secret key.
                context.RouteData.TryGetReceiverId(out var id);
                var secretKey = await GetReceiverConfig(request, ReceiverName, id, SecretMinLength, SecretMaxLength);
                var secret = Encoding.UTF8.GetBytes(secretKey);

                // 3. Get the actual hash of the request body.
                await PrepareRequestBody(request);

                byte[] actualHash;
                using (var hasher = new HMACSHA1(secret))
                {
                    try
                    {
                        actualHash = hasher.ComputeHash(request.Body);
                    }
                    finally
                    {
                        // Reset Position because JsonInputFormatter et cetera always start from current position.
                        request.Body.Seek(0L, SeekOrigin.Begin);
                    }
                }

                // 4. Verify that the actual hash matches the expected hash.
                if (!SecretEqual(expectedHash, actualHash))
                {
                    // Log about the issue and short-circuit remainder of the pipeline.
                    var badSignature = CreateBadSignatureResult(request, SignatureHeaderName);

                    context.Result = badSignature;
                    return;
                }
            }

            await next();
        }
    }
}
