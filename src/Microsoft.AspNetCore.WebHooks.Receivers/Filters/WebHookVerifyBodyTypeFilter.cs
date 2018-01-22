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
    /// action's  <see cref="IWebHookBodyTypeMetadata.BodyType"/> and / or the receiver's
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
        private readonly IWebHookBodyTypeMetadata _bodyTypeMetadata;
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifyMethodFilter"/> instance to verify the given action- or
        /// receiver-specific <paramref name="bodyTypeMetadata"/>.
        /// </summary>
        /// <param name="bodyTypeMetadata">
        /// The <see cref="IWebHookBodyTypeMetadata"/> to confirm matches the request's <c>Content-Type</c>.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebHookVerifyBodyTypeFilter(IWebHookBodyTypeMetadata bodyTypeMetadata, ILoggerFactory loggerFactory)
        {
            if (bodyTypeMetadata == null)
            {
                throw new ArgumentNullException(nameof(bodyTypeMetadata));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _bodyTypeMetadata = bodyTypeMetadata;
            _logger = loggerFactory.CreateLogger<WebHookVerifyBodyTypeFilter>();
        }

        /// <summary>
        /// Instantiates a new <see cref="WebHookVerifyMethodFilter"/> instance to verify the given action-specific
        /// <paramref name="actionBodyTypeMetadata"/>. Also confirms <paramref name="actionBodyTypeMetadata"/> is
        /// <see cref="WebHookBodyType.All"/> or a subset of the <see cref="IWebHookBodyTypeMetadataService"/> found in
        /// <paramref name="allBodyTypeMetadata"/> for the receiver handling the request.
        /// </summary>
        /// <param name="actionBodyTypeMetadata">
        /// The <see cref="IWebHookBodyTypeMetadata"/> to confirm matches the request's <c>Content-Type</c>.
        /// </param>
        /// <param name="allBodyTypeMetadata">
        /// The collection of <see cref="IWebHookBodyTypeMetadataService"/> services. Searched for applicable metadata
        /// per-request.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <remarks>
        /// This overload is intended for use with <see cref="GeneralWebHookAttribute"/>.
        /// </remarks>
        public WebHookVerifyBodyTypeFilter(
            IWebHookBodyTypeMetadata actionBodyTypeMetadata,
            IReadOnlyList<IWebHookBodyTypeMetadataService> allBodyTypeMetadata,
            ILoggerFactory loggerFactory)
            : this (actionBodyTypeMetadata, loggerFactory)
        {
            if (allBodyTypeMetadata == null)
            {
                throw new ArgumentNullException(nameof(allBodyTypeMetadata));
            }

            _allBodyTypeMetadata = allBodyTypeMetadata;
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

            var bodyTypeMetadata = _bodyTypeMetadata;
            if (_allBodyTypeMetadata != null)
            {
                var receiverBodyTypeMetadata = _allBodyTypeMetadata
                    .FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (receiverBodyTypeMetadata == null)
                {
                    // WebHookMetadataProvider requires an IWebHookBodyTypeMetadataService implementation for all
                    // receivers. But, this case may occur if user has no receiver-specific actions i.e. uses only the
                    // GeneralWebHookAttribute.
                    _logger.LogCritical(
                        0,
                        "No '{MetadataType}' implementation found for the '{ReceiverName}' WebHook receiver. Each " +
                        "receiver must register a '{ServiceMetadataType}' service.",
                        typeof(IWebHookBodyTypeMetadataService),
                        receiverName,
                        typeof(IWebHookBodyTypeMetadataService));

                    // Reuse the message for the Exception the WebHookMetadataProvider should have thrown.
                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Shared_MissingMetadata,
                        typeof(IWebHookBodyTypeMetadataService),
                        receiverName);
                    throw new InvalidOperationException(message);
                }

                if (bodyTypeMetadata.BodyType == WebHookBodyType.All)
                {
                    // Use receiver-specific requirement since the action is flexible.
                    bodyTypeMetadata = receiverBodyTypeMetadata;
                }
                else if ((~receiverBodyTypeMetadata.BodyType & bodyTypeMetadata.BodyType) != 0)
                {
                    // Failed subset check that WebHookMetadataProvider could not perform: Attribute must require the
                    // same body type as receiver's metadata service or a subset. That is, `bodyTypeMetadata.BodyType`
                    // flags must not include any beyond those set in `receiverBodyTypeMetadata.BodyType`.
                    _logger.LogCritical(
                        2,
                        "Invalid '{MetadataType}.{PropertyName}' value '{PropertyValue}' in {AttributeType}. " +
                        "This value must be equal to or a subset of the " +
                        "'{ServiceMetadataType}.{ServicePropertyName}' value '{ServicePropertyValue}' for the " +
                        "'{ReceiverName}' WebHook receiver.",
                        typeof(IWebHookBodyTypeMetadata),
                        nameof(IWebHookBodyTypeMetadata.BodyType),
                        bodyTypeMetadata.BodyType,
                        bodyTypeMetadata.GetType(),
                        typeof(IWebHookBodyTypeMetadataService),
                        nameof(IWebHookBodyTypeMetadataService.BodyType),
                        receiverBodyTypeMetadata.BodyType,
                        receiverName);

                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.Shared_InvalidAttributeValue,
                        bodyTypeMetadata.GetType(),
                        typeof(IWebHookBodyTypeMetadata),
                        nameof(IWebHookBodyTypeMetadata.BodyType));
                    throw new InvalidOperationException(message);
                }
            }

            var request = context.HttpContext.Request;
            switch (bodyTypeMetadata.BodyType)
            {
                case WebHookBodyType.Form:
                    if (!request.HasFormContentType)
                    {
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_NoFormData,
                            receiverName,
                            request.GetTypedHeaders().ContentType);
                        context.Result = CreateUnsupportedMediaTypeResult(message);
                    }
                    break;

                case WebHookBodyType.Json:
                    if (!RequestBodyTypes.IsJson(request))
                    {
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_NoJson,
                            receiverName,
                            request.GetTypedHeaders().ContentType);
                        context.Result = CreateUnsupportedMediaTypeResult(message);
                    }
                    break;

                case WebHookBodyType.Xml:
                    if (!RequestBodyTypes.IsXml(request))
                    {
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_NoXml,
                            receiverName,
                            request.GetTypedHeaders().ContentType);
                        context.Result = CreateUnsupportedMediaTypeResult(message);
                    }
                    break;

                default:
                    // Multiple flags set is a special case. Occurs when receiver supports multiple body types and
                    // action has no more specific requirements i.e. its BodyType is `All`.
                    if ((WebHookBodyType.Form & bodyTypeMetadata.BodyType) != 0 && request.HasFormContentType)
                    {
                        return;
                    }

                    if ((WebHookBodyType.Json & bodyTypeMetadata.BodyType) != 0 && RequestBodyTypes.IsJson(request))
                    {
                        return;
                    }

                    if ((WebHookBodyType.Xml & bodyTypeMetadata.BodyType) != 0 && RequestBodyTypes.IsXml(request))
                    {
                        return;
                    }

                    {
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.VerifyBody_UnsupportedContentType,
                            receiverName,
                            request.GetTypedHeaders().ContentType,
                            bodyTypeMetadata.BodyType);
                        context.Result = CreateUnsupportedMediaTypeResult(message);
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
            // No-op
        }

        private IActionResult CreateUnsupportedMediaTypeResult(string message)
        {
            _logger.LogError(3, message);
            var badMethod = new BadRequestObjectResult(message)
            {
                StatusCode = StatusCodes.Status415UnsupportedMediaType
            };

            return badMethod;
        }
    }
}
