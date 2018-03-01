// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    /// <summary>
    /// An <see cref="IResourceFilter"/> to allow only WebHook requests with a <c>Content-Type</c> matching the
    /// action's <see cref="IWebHookBodyTypeMetadata.BodyType"/> and / or the receiver's
    /// <see cref="IWebHookBodyTypeMetadataService.BodyType"/>.
    /// </summary>
    /// <remarks>
    /// Done as an <see cref="IResourceFilter"/> implementation and not an
    /// <see cref="Mvc.ActionConstraints.IActionConstraintMetadata"/> because receivers do not dynamically vary their
    /// <see cref="IWebHookBodyTypeMetadata"/>. Use distinct <see cref="WebHookAttribute.Id"/> values if different
    /// configurations are needed for one receiver and the receiver's <see cref="WebHookAttribute"/> implements
    /// <see cref="IWebHookBodyTypeMetadata"/>.
    /// </remarks>
    public class WebHookVerifyBodyTypeFilter : IResourceFilter, IOrderedFilter
    {
        private readonly IReadOnlyList<IWebHookBodyTypeMetadataService> _allBodyTypeMetadata;
        private readonly IWebHookBodyTypeMetadataService _receiverBodyTypeMetadata;
        private readonly IWebHookBodyTypeMetadata _actionBodyTypeMetadata;
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifyMethodFilter"/> instance to verify the given
        /// <paramref name="receiverBodyTypeMetadata"/> or <paramref name="actionBodyTypeMetadata"/>.
        /// </summary>
        /// <param name="receiverBodyTypeMetadata">
        /// The receiver's <see cref="IWebHookBodyTypeMetadataService"/>.
        /// </param>
        /// <param name="actionBodyTypeMetadata">The action's <see cref="IWebHookBodyTypeMetadata"/>, if any.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebHookVerifyBodyTypeFilter(
            IWebHookBodyTypeMetadataService receiverBodyTypeMetadata,
            IWebHookBodyTypeMetadata actionBodyTypeMetadata,
            ILoggerFactory loggerFactory)
        {
            if (receiverBodyTypeMetadata == null)
            {
                throw new ArgumentNullException(nameof(receiverBodyTypeMetadata));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            // Ignore AllBodyTypes metadata for the action.
            if (actionBodyTypeMetadata != null && actionBodyTypeMetadata.BodyType != WebHookConstants.AllBodyTypes)
            {
                _actionBodyTypeMetadata = actionBodyTypeMetadata;
            }

            _receiverBodyTypeMetadata = receiverBodyTypeMetadata;
            _logger = loggerFactory.CreateLogger<WebHookVerifyBodyTypeFilter>();
        }

        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifyMethodFilter"/> instance to verify the receiver's
        /// <see cref="WebHookBodyType"/> (found in <paramref name="allBodyTypeMetadata"/>) or the given
        /// <paramref name="actionBodyTypeMetadata"/>. Also confirms <paramref name="actionBodyTypeMetadata"/> is
        /// <see langword="null"/>, <see cref="WebHookConstants.AllBodyTypes"/>, or a subset of the receiver's
        /// <see cref="IWebHookBodyTypeMetadataService"/>.
        /// </summary>
        /// <param name="allBodyTypeMetadata">
        /// The collection of <see cref="IWebHookBodyTypeMetadataService"/> services. Searched for applicable metadata
        /// per-request.
        /// </param>
        /// <param name="actionBodyTypeMetadata">The action's <see cref="IWebHookBodyTypeMetadata"/>, if any.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <remarks>
        /// This overload is intended for use with <see cref="GeneralWebHookAttribute"/>.
        /// </remarks>
        public WebHookVerifyBodyTypeFilter(
            IReadOnlyList<IWebHookBodyTypeMetadataService> allBodyTypeMetadata,
            IWebHookBodyTypeMetadata actionBodyTypeMetadata,
            ILoggerFactory loggerFactory)
        {
            if (allBodyTypeMetadata == null)
            {
                throw new ArgumentNullException(nameof(allBodyTypeMetadata));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            // Ignore AllBodyTypes metadata for the action.
            if (actionBodyTypeMetadata != null && actionBodyTypeMetadata.BodyType != WebHookConstants.AllBodyTypes)
            {
                _actionBodyTypeMetadata = actionBodyTypeMetadata;
            }

            _allBodyTypeMetadata = allBodyTypeMetadata;
            _logger = loggerFactory.CreateLogger<WebHookVerifyBodyTypeFilter>();
        }

        /// <summary>
        /// Gets the <see cref="IOrderedFilter.Order"/> used in all <see cref="WebHookVerifyBodyTypeFilter"/>
        /// instances. The recommended filter sequence is
        /// <list type="number">
        /// <item>
        /// Confirm signature or <c>code</c> query parameter e.g. in <see cref="WebHookVerifyCodeFilter"/> or other
        /// <see cref="WebHookSecurityFilter"/> subclass.
        /// </item>
        /// <item>
        /// Confirm required headers, <see cref="RouteValueDictionary"/> entries and query parameters are provided
        /// (in <see cref="WebHookVerifyRequiredValueFilter"/>).
        /// </item>
        /// <item>
        /// Short-circuit GET or HEAD requests, if receiver supports either (in
        /// <see cref="WebHookGetHeadRequestFilter"/>).
        /// </item>
        /// <item>Confirm it's a POST request (in <see cref="WebHookVerifyMethodFilter"/>).</item>
        /// <item>Confirm body type (in this filter).</item>
        /// <item>
        /// Map event name(s), if not done in <see cref="Routing.WebHookEventMapperConstraint"/> for this receiver (in
        /// <see cref="WebHookEventMapperFilter"/>).
        /// </item>
        /// <item>
        /// Short-circuit ping requests, if not done in <see cref="WebHookGetHeadRequestFilter"/> for this receiver (in
        /// <see cref="WebHookPingRequestFilter"/>).
        /// </item>
        /// </list>
        /// </summary>
        public static int Order => WebHookVerifyMethodFilter.Order + 10;

        /// <inheritdoc />
        int IOrderedFilter.Order => Order;

        /// <inheritdoc />
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var routeData = context.RouteData;
            if (!routeData.TryGetWebHookReceiverName(out var receiverName))
            {
                return;
            }

            var bodyType = _actionBodyTypeMetadata?.BodyType ??
                _receiverBodyTypeMetadata?.BodyType ??
                WebHookConstants.AllBodyTypes;

            if (_allBodyTypeMetadata != null)
            {
                // WebHookReceiverExistsConstraint confirms the IWebHookBodyTypeMetadataService implementation exists.
                var receiverBodyTypeMetadata = _allBodyTypeMetadata
                    .First(metadata => metadata.IsApplicable(receiverName));
                if (bodyType == WebHookConstants.AllBodyTypes)
                {
                    // Use receiver-specific requirement since the action is flexible.
                    bodyType = receiverBodyTypeMetadata.BodyType;
                }
                else if ((~receiverBodyTypeMetadata.BodyType & bodyType) != 0)
                {
                    // Failed subset check that WebHookMetadataProvider could not perform: Attribute must require the
                    // same body type as receiver's metadata service or a subset. That is, bodyTypeMetadata.BodyType
                    // flags must be AllBodyTypes (_actionBodyTypeMetadata is null in that case) or must not include
                    // any beyond those set in receiverBodyTypeMetadata.BodyType.
                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Shared_NonSubsetAttributeBodyType,
                        _actionBodyTypeMetadata.GetType(),
                        typeof(IWebHookBodyTypeMetadata),
                        nameof(IWebHookBodyTypeMetadata.BodyType),
                        bodyType,
                        WebHookConstants.AllBodyTypes,
                        receiverBodyTypeMetadata.BodyType);
                    throw new InvalidOperationException(message);
                }
            }

            var request = context.HttpContext.Request;
            switch (bodyType)
            {
                case WebHookBodyType.Form:
                    if (!request.HasFormContentType)
                    {
                        var contentType = request.GetTypedHeaders().ContentType;
                        _logger.LogWarning(
                            0,
                            "The '{ReceiverName}' WebHook receiver does not support content type '{ContentType}'. " +
                            "The WebHook request must contain an entity body formatted as HTML form URL-encoded data.",
                            receiverName,
                            contentType);
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_NoFormData,
                            receiverName,
                            contentType);
                        context.Result = new BadRequestObjectResult(message)
                        {
                            StatusCode = StatusCodes.Status415UnsupportedMediaType
                        };
                    }
                    break;

                case WebHookBodyType.Json:
                    if (!RequestBodyTypes.IsJson(request))
                    {
                        var contentType = request.GetTypedHeaders().ContentType;
                        _logger.LogWarning(
                            1,
                            "The '{ReceiverName}' WebHook receiver does not support content type '{ContentType}'. " +
                            "The WebHook request must contain an entity body formatted as JSON.",
                            receiverName,
                            contentType);
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_NoJson,
                            receiverName,
                            contentType);
                        context.Result = new BadRequestObjectResult(message)
                        {
                            StatusCode = StatusCodes.Status415UnsupportedMediaType
                        };
                    }
                    break;

                case WebHookBodyType.Xml:
                    if (!RequestBodyTypes.IsXml(request))
                    {
                        var contentType = request.GetTypedHeaders().ContentType;
                        _logger.LogWarning(
                            2,
                            "The '{ReceiverName}' WebHook receiver does not support content type '{ContentType}'. " +
                            "The WebHook request must contain an entity body formatted as XML.",
                            receiverName,
                            contentType);
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_NoXml,
                            receiverName,
                            contentType);
                        context.Result = new BadRequestObjectResult(message)
                        {
                            StatusCode = StatusCodes.Status415UnsupportedMediaType
                        };
                    }
                    break;

                default:
                    // Multiple flags set is a special case. Occurs when receiver supports multiple body types and
                    // action has no more specific requirements i.e. its BodyType is AllBodyTypes.
                    if ((WebHookBodyType.Form & bodyType) != 0 && request.HasFormContentType)
                    {
                        return;
                    }

                    if ((WebHookBodyType.Json & bodyType) != 0 && RequestBodyTypes.IsJson(request))
                    {
                        return;
                    }

                    if ((WebHookBodyType.Xml & bodyType) != 0 && RequestBodyTypes.IsXml(request))
                    {
                        return;
                    }

                    {
                        var contentType = request.GetTypedHeaders().ContentType;
                        _logger.LogWarning(
                            3,
                            "The '{ReceiverName}' WebHook receiver does not support content type '{ContentType}'. " +
                            "The WebHook request must contain an entity body formatted to match {BodyType}.",
                            receiverName,
                            contentType,
                            bodyType);
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_UnsupportedContentType,
                            receiverName,
                            contentType,
                            bodyType);
                        context.Result = new BadRequestObjectResult(message)
                        {
                            StatusCode = StatusCodes.Status415UnsupportedMediaType
                        };
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }
    }
}
