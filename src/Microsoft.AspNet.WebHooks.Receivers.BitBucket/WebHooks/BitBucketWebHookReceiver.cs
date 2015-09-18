﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using Newtonsoft.Json.Linq;
using Microsoft.AspNet.WebHooks.Properties;

//ToDo - this code is not tested, just a thought to create WebHook for BitBucket
namespace Microsoft.AspNet.WebHooks
{
    /// <summary>
    /// Provides an <see cref="IWebHookReceiver"/> implementation which supports WebHooks generated by BitBucket. 
    /// Set the '<c>MS_WebHookReceiverSecret_BitBucket</c>' application setting to the secret defined in BitBucket.
    /// The corresponding WebHook URI is of the form '<c>https://&lt;host&gt;/api/webhooks/incoming/BitBucket</c>'.
    /// For details about BitBucket WebHooks, see <c>https://confluence.atlassian.com/bitbucket/manage-webhooks-735643732.html/</c>.
    /// </summary>
    public class BitBucketWebHookReceiver : WebHookReceiver
    {
        internal const string SecretKey = "MS_WebHookReceiverSecret_BitBucket";

        internal const string SignatureHeaderKey = "sha1";
        internal const string SignatureHeaderValueTemplate = SignatureHeaderKey + "={0}";
        internal const string EventHeaderKey = "X-Event-Key";
        internal const string UUIDHeaderName = "X-Hook-UUID";
        internal const string PingEvent = "ping";

        private static readonly string[] ReceiverNames = new string[] { "BitBucket" };

        /// <inheritdoc />
        public override IEnumerable<string> Names
        {
            get { return ReceiverNames; }
        }

        /// <inheritdoc />
        public override async Task<HttpResponseMessage> ReceiveAsync(string receiver, HttpRequestContext context, HttpRequestMessage request)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (request.Method == HttpMethod.Post)
            {
                await VerifySignature(request);

                // Read the request entity body.
                JObject data = await ReadAsJsonAsync(request);

                // Pick out action from headers
                IEnumerable<string> actions;
                if (!request.Headers.TryGetValues(UUIDHeaderName, out actions))
                {
                    string msg = string.Format(CultureInfo.CurrentCulture, BitBucketReceiverResources.Receiver_NoEvent, UUIDHeaderName);
                    context.Configuration.DependencyResolver.GetLogger().Error(msg);
                    HttpResponseMessage noHeader = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                    return noHeader;
                }

                // If this is a ping request then just return. Otherwise call handlers.
                if (string.Equals(actions.FirstOrDefault(), PingEvent, StringComparison.OrdinalIgnoreCase))
                {
                    return request.CreateResponse();
                }
                return await ExecuteWebHookAsync(receiver, context, request, actions, data);
            }
            else
            {
                return CreateBadMethodResponse(request);
            }
        }

        /// <summary>
        /// Verifies that the signature header matches that of the actual body.
        /// </summary>
        protected virtual async Task VerifySignature(HttpRequestMessage request)
        {
            string secretKey = GetWebHookSecret(request, SecretKey, 8, 64);

            // Get the expected hash from the signature header
            string header = GetRequestHeader(request, EventHeaderKey);
            string[] values = header.SplitAndTrim('=');
            if (values.Length != 2 || !string.Equals(values[0], SignatureHeaderKey, StringComparison.OrdinalIgnoreCase))
            {
                string msg = string.Format(CultureInfo.CurrentCulture, BitBucketReceiverResources.Receiver_BadHeaderValue, EventHeaderKey, SignatureHeaderKey, "<value>");
                request.GetConfiguration().DependencyResolver.GetLogger().Error(msg);
                HttpResponseMessage invalidHeader = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(invalidHeader);
            }

            byte[] expectedHash;
            try
            {
                expectedHash = EncodingUtilities.FromHex(values[1]);
            }
            catch (Exception ex)
            {
                string msg = string.Format(CultureInfo.CurrentCulture, BitBucketReceiverResources.Receiver_BadHeaderEncoding, EventHeaderKey);
                request.GetConfiguration().DependencyResolver.GetLogger().Error(msg, ex);
                HttpResponseMessage invalidEncoding = request.CreateErrorResponse(HttpStatusCode.BadRequest, msg);
                throw new HttpResponseException(invalidEncoding);
            }

            // Get the actual hash of the request body
            byte[] actualHash;
            byte[] secret = Encoding.UTF8.GetBytes(secretKey);
            using (var hasher = new HMACSHA1(secret))
            {
                byte[] data = await request.Content.ReadAsByteArrayAsync();
                actualHash = hasher.ComputeHash(data);
            }

            // Now verify that the provided hash matches the expected hash.
            if (!WebHookReceiver.SecretEqual(expectedHash, actualHash))
            {
                HttpResponseMessage badSignature = CreateBadSignatureResponse(request, EventHeaderKey);
                throw new HttpResponseException(badSignature);
            }
        }
    }
}
