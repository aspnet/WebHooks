// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.ApplicationModels
{
    /// <summary>
    /// <para>
    /// An <see cref="IApplicationModelProvider"/> implementation that adds <see cref="IWebHookMetadata"/>
    /// references to WebHook <see cref="ActionModel"/>s. Metadata is stored in <see cref="ActionModel.Properties"/>
    /// and used in <see cref="WebHookModelBindingProvider"/> and <see cref="WebHookRoutingProvider"/>.
    /// </para>
    /// <para>
    /// Detects missing and duplicate <see cref="IWebHookMetadata"/> services.
    /// </para>
    /// </summary>
    public class WebHookMetadataProvider : IApplicationModelProvider
    {
        private readonly IReadOnlyList<IWebHookBindingMetadata> _bindingMetadata;
        private readonly IReadOnlyList<IWebHookBodyTypeMetadataService> _bodyTypeMetadata;
        private readonly IReadOnlyList<IWebHookEventMetadata> _eventMetadata;
        private readonly IReadOnlyList<IWebHookPingRequestMetadata> _pingRequestMetadata;
        private readonly ILogger _logger;

        /// <summary>
        /// Instantiates a new <see cref="WebHookMetadataProvider"/> instance with the given metadata.
        /// </summary>
        /// <param name="bindingMetadata">The collection of <see cref="IWebHookBindingMetadata"/> services.</param>
        /// <param name="bodyTypeMetadata">
        /// The collection of <see cref="IWebHookBodyTypeMetadataService"/> services.
        /// </param>
        /// <param name="eventFromBodyMetadata">
        /// The collection of <see cref="IWebHookEventFromBodyMetadata"/> services.
        /// </param>
        /// <param name="eventMetadata">The collection of <see cref="IWebHookEventMetadata"/> services.</param>
        /// <param name="getHeadRequestMetadata">
        /// The collection of <see cref="IWebHookGetHeadRequestMetadata"/> services.
        /// </param>
        /// <param name="pingRequestMetadata">
        /// The collection of <see cref="IWebHookPingRequestMetadata"/> services.
        /// </param>
        /// <param name="verifyCodeMetadata">
        /// The collection of <see cref="IWebHookVerifyCodeMetadata"/> services.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebHookMetadataProvider(
            IEnumerable<IWebHookBindingMetadata> bindingMetadata,
            IEnumerable<IWebHookBodyTypeMetadataService> bodyTypeMetadata,
            IEnumerable<IWebHookEventFromBodyMetadata> eventFromBodyMetadata,
            IEnumerable<IWebHookEventMetadata> eventMetadata,
            IEnumerable<IWebHookGetHeadRequestMetadata> getHeadRequestMetadata,
            IEnumerable<IWebHookPingRequestMetadata> pingRequestMetadata,
            IEnumerable<IWebHookVerifyCodeMetadata> verifyCodeMetadata,
            ILoggerFactory loggerFactory)
        {
            _bindingMetadata = bindingMetadata.ToArray();
            _bodyTypeMetadata = bodyTypeMetadata.ToArray();
            _eventMetadata = eventMetadata.ToArray();
            _pingRequestMetadata = pingRequestMetadata.ToArray();
            _logger = loggerFactory.CreateLogger<WebHookMetadataProvider>();

            // Check for duplicate registrations in the collections tracked here.
            EnsureUniqueRegistrations(_bindingMetadata);
            EnsureUniqueRegistrations(_bodyTypeMetadata);
            EnsureUniqueRegistrations(_eventMetadata);
            EnsureUniqueRegistrations(_pingRequestMetadata);

            // Check for duplicates in other metadata registrations.
            var eventFromBodyMetadataArray = eventFromBodyMetadata.ToArray();
            EnsureUniqueRegistrations(eventFromBodyMetadataArray);
            EnsureUniqueRegistrations(getHeadRequestMetadata.ToArray());
            EnsureUniqueRegistrations(verifyCodeMetadata.ToArray());

            EnsureValidBodyTypeMetadata(_bodyTypeMetadata);
            EnsureValidEventFromBodyMetadata(eventFromBodyMetadataArray, _eventMetadata);
        }

        /// <summary>
        /// Gets the <see cref="IApplicationModelProvider.Order"/> value used in all
        /// <see cref="WebHookMetadataProvider"/> instances. The recommended <see cref="IApplicationModelProvider"/>
        /// order is
        /// <list type="number">
        /// <item>
        /// Validate metadata services and <see cref="WebHookAttribute"/> metadata implementations and add information
        /// used in later application model providers (in this provider).
        /// </item>
        /// <item>
        /// Add routing information (template, constraints and filters) to <see cref="ActionModel"/>s (in
        /// <see cref="WebHookRoutingProvider"/>).
        /// </item>
        /// <item>
        /// Add model binding information (<see cref="Mvc.ModelBinding.BindingInfo"/> settings) to
        /// <see cref="ParameterModel"/>s (in <see cref="WebHookModelBindingProvider"/>).
        /// </item>
        /// </list>
        /// </summary>
        /// <value>
        /// Chosen to ensure WebHook providers run after MVC's
        /// <see cref="Mvc.Internal.DefaultApplicationModelProvider"/>.
        /// </value>
        public static int Order => -500;

        /// <inheritdoc />
        int IApplicationModelProvider.Order => Order;

        /// <inheritdoc />
        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            for (var i = 0; i < context.Result.Controllers.Count; i++)
            {
                var controller = context.Result.Controllers[i];
                for (var j = 0; j < controller.Actions.Count; j++)
                {
                    var action = controller.Actions[j];
                    Apply(action);
                }
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            // No-op
        }

        private void Apply(ActionModel action)
        {
            var attribute = action.Attributes.OfType<WebHookAttribute>().FirstOrDefault();
            if (attribute == null)
            {
                // Not a WebHook handler.
                return;
            }

            var receiverName = attribute.ReceiverName;
            if (receiverName != null)
            {
                var bindingMetadata = _bindingMetadata.FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (bindingMetadata != null)
                {
                    action.Properties[typeof(IWebHookBindingMetadata)] = bindingMetadata;
                }
            }

            IWebHookEventMetadata eventMetadata;
            if (receiverName == null)
            {
                // Pass along all IWebHookEventMetadata and IWebHookPingRequestMetadata instances.
                eventMetadata = null;
                action.Properties[typeof(IWebHookEventMetadata)] = _eventMetadata;
                action.Properties[typeof(IWebHookPingRequestMetadata)] = _pingRequestMetadata;
            }
            else
            {
                eventMetadata = _eventMetadata.FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (eventMetadata != null)
                {
                    action.Properties[typeof(IWebHookEventMetadata)] = eventMetadata;
                }

                var pingRequestMetadata = _pingRequestMetadata
                    .FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (pingRequestMetadata != null)
                {
                    action.Properties[typeof(IWebHookPingRequestMetadata)] = pingRequestMetadata;
                }
            }

            if (attribute is IWebHookEventSelectorMetadata eventSelector &&
                eventSelector.EventName != null)
            {
                EnsureValidEventMetadata(eventMetadata, receiverName);
                action.Properties[typeof(IWebHookEventSelectorMetadata)] = eventSelector;
            }

            IWebHookBodyTypeMetadataService receiverBodyTypeMetadata;
            if (receiverName == null)
            {
                // WebHookVerifyBodyTypeFilter should look up (and verify) the applicable
                // IWebHookBodyTypeMetadataService per-request.
                receiverBodyTypeMetadata = null;
                action.Properties[typeof(IWebHookBodyTypeMetadataService)] = _bodyTypeMetadata;
            }
            else
            {
                receiverBodyTypeMetadata = _bodyTypeMetadata
                    .FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                EnsureValidBodyTypeMetadata(receiverBodyTypeMetadata, receiverName);
                action.Properties[typeof(IWebHookBodyTypeMetadata)] = receiverBodyTypeMetadata;
            }

            // Override the WebHookBodyType if the WebHookAttribute provides that information. At least as narrow as
            // the receiver's requirements when both are known.
            if (attribute is IWebHookBodyTypeMetadata actionBodyTypeMetadata)
            {
                EnsureValidBodyTypeMetadata(actionBodyTypeMetadata, receiverBodyTypeMetadata);
                action.Properties[typeof(IWebHookBodyTypeMetadata)] = actionBodyTypeMetadata;
            }
        }

        /// <summary>
        /// Ensure members of given <paramref name="bodyTypeMetadata"/> collection are valid. That is, ensure each
        /// has a valid <see cref="IWebHookBodyTypeMetadataService.BodyType"/>.
        /// </summary>
        /// <param name="bodyTypeMetadata">
        /// The collection of <see cref="IWebHookBodyTypeMetadataService"/> services.
        /// </param>
        protected void EnsureValidBodyTypeMetadata(IReadOnlyList<IWebHookBodyTypeMetadataService> bodyTypeMetadata)
        {
            if (bodyTypeMetadata == null)
            {
                throw new ArgumentNullException(nameof(bodyTypeMetadata));
            }

            var invalidRegistrations = false;
            foreach (var receiverBodyTypeMetadata in bodyTypeMetadata)
            {
                // Confirm the receiver's BodyType is valid.
                if (receiverBodyTypeMetadata.BodyType == 0)
                {
                    invalidRegistrations = true;
                    _logger.LogCritical(
                        0,
                        "Invalid '{MetadataType}.{PropertyName}' value '0' for the '{ReceiverName}' " +
                        "WebHook receiver. Must have at least one {BodyType} flag set.",
                        typeof(IWebHookBodyTypeMetadataService),
                        nameof(IWebHookBodyTypeMetadataService.BodyType),
                        receiverBodyTypeMetadata.ReceiverName,
                        nameof(WebHookBodyType));
                }
                else if ((~WebHookBodyType.All & receiverBodyTypeMetadata.BodyType) != 0)
                {
                    // Value contains undefined flags.
                    invalidRegistrations = true;
                    _logger.LogCritical(
                        0,
                        "Invalid '{MetadataType}.{PropertyName}' value '{PropertyValue}' for the '{ReceiverName}' " +
                        "WebHook receiver. Enum type {BodyType} has no matching defined value.",
                        typeof(IWebHookBodyTypeMetadataService),
                        nameof(IWebHookBodyTypeMetadataService.BodyType),
                        receiverBodyTypeMetadata.BodyType,
                        receiverBodyTypeMetadata.ReceiverName,
                        nameof(WebHookBodyType));
                }
            }

            if (invalidRegistrations)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.MetadataProvider_InvalidMetadataServiceValue,
                    typeof(IWebHookBodyTypeMetadataService),
                    nameof(IWebHookBodyTypeMetadataService.BodyType));
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Ensure given <paramref name="receiverBodyTypeMetadata"/> is not <see langword="null"/>.
        /// An <see cref="IWebHookBodyTypeMetadataService"/> service is mandatory for every receiver.
        /// </summary>
        /// <param name="receiverBodyTypeMetadata">
        /// The <paramref name="receiverName"/> receiver's <see cref="IWebHookBodyTypeMetadataService"/>, if any.
        /// </param>
        /// <param name="receiverName">The name of an available <see cref="IWebHookReceiver"/>.</param>
        protected void EnsureValidBodyTypeMetadata(
            IWebHookBodyTypeMetadataService receiverBodyTypeMetadata,
            string receiverName)
        {
            if (receiverName == null)
            {
                throw new ArgumentNullException(nameof(receiverName));
            }

            if (receiverBodyTypeMetadata == null)
            {
                _logger.LogCritical(
                    2,
                    "No '{MetadataType}' implementation found for the '{ReceiverName}' WebHook receiver. Each " +
                    "receiver must register a '{ServiceMetadataType}' service.",
                    typeof(IWebHookBodyTypeMetadataService),
                    receiverName,
                    typeof(IWebHookBodyTypeMetadataService));

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Shared_MissingMetadata,
                    typeof(IWebHookBodyTypeMetadataService),
                    receiverName);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Ensure given <paramref name="actionBodyTypeMetadata"/> is valid and consistent with given
        /// <paramref name="receiverBodyTypeMetadata"/>.
        /// </summary>
        /// <param name="actionBodyTypeMetadata">
        /// An attribute that implements <see cref="IWebHookBodyTypeMetadata"/>.
        /// </param>
        /// <param name="receiverBodyTypeMetadata">
        /// The corresponding receiver's <see cref="IWebHookBodyTypeMetadataService"/>, if any.
        /// </param>
        /// <remarks>
        /// <paramref name="receiverBodyTypeMetadata"/> is <see langword="null"/> only if
        /// <paramref name="actionBodyTypeMetadata"/> is a <see cref="GeneralWebHookAttribute"/> instance.
        /// </remarks>
        protected void EnsureValidBodyTypeMetadata(
            IWebHookBodyTypeMetadata actionBodyTypeMetadata,
            IWebHookBodyTypeMetadataService receiverBodyTypeMetadata)
        {
            if (actionBodyTypeMetadata == null)
            {
                throw new ArgumentNullException(nameof(actionBodyTypeMetadata));
            }

            // Confirm the attribute's BodyType is valid on its own. Avoid Enum.IsDefined because we want to
            // distinguish invalid flag combinations from undefined flags.
            switch (actionBodyTypeMetadata.BodyType)
            {
                case WebHookBodyType.All:
                case WebHookBodyType.Form:
                case WebHookBodyType.Json:
                case WebHookBodyType.Xml:
                    // Just right.
                    break;

                case 0:
                case WebHookBodyType.Form | WebHookBodyType.Json:
                case WebHookBodyType.Form | WebHookBodyType.Xml:
                case WebHookBodyType.Json | WebHookBodyType.Xml:
                    {
                        // 0 or contains an invalid combination of flags.
                        _logger.LogCritical(
                            3,
                            "Invalid '{MetadataType}.{PropertyName}' value '{PropertyValue}' in " +
                            "{AttributeType}. This value must have a single {BodyType} flag set or be '{AllValue}'.",
                            typeof(IWebHookBodyTypeMetadata),
                            nameof(IWebHookBodyTypeMetadata.BodyType),
                            actionBodyTypeMetadata.BodyType,
                            actionBodyTypeMetadata.GetType(),
                            nameof(WebHookBodyType),
                            WebHookBodyType.All);

                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.MetadataProvider_InvalidBodyType,
                            actionBodyTypeMetadata.GetType(),
                            typeof(IWebHookBodyTypeMetadata),
                            nameof(IWebHookBodyTypeMetadata.BodyType),
                            nameof(WebHookBodyType),
                            WebHookBodyType.All);
                        throw new InvalidOperationException(message);
                    }

                default:
                    {
                        // Value contains undefined flags.
                        _logger.LogCritical(
                            4,
                            "Invalid '{MetadataType}.{PropertyName}' value '{PropertyValue}' in " +
                            "{AttributeType}. Enum type {BodyType} has no matching defined value.",
                            typeof(IWebHookBodyTypeMetadata),
                            nameof(IWebHookBodyTypeMetadata.BodyType),
                            actionBodyTypeMetadata.BodyType,
                            actionBodyTypeMetadata.GetType(),
                            nameof(WebHookBodyType));

                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.General_InvalidEnumValue,
                            nameof(WebHookBodyType),
                            actionBodyTypeMetadata.BodyType);
                        throw new InvalidOperationException(message);
                    }
            }

            // Attribute must require the same body type as receiver's metadata service or a subset. That is,
            // `actionBodyTypeMetadata.BodyType` flags must not include any beyond those set in
            // `receiverBodyTypeMetadata.BodyType`.
            if (receiverBodyTypeMetadata != null &&
                (~receiverBodyTypeMetadata.BodyType & actionBodyTypeMetadata.BodyType) != 0)
            {
                _logger.LogCritical(
                    5,
                    "Invalid '{MetadataType}.{PropertyName}' value '{PropertyValue}' in {AttributeType}. This " +
                    "value must be equal to or a subset of the '{ServiceMetadataType}.{ServicePropertyName}' " +
                    "value '{ServicePropertyValue}' for the '{ReceiverName}' WebHook receiver.",
                    typeof(IWebHookBodyTypeMetadata),
                    nameof(IWebHookBodyTypeMetadata.BodyType),
                    actionBodyTypeMetadata.BodyType,
                    actionBodyTypeMetadata.GetType(),
                    typeof(IWebHookBodyTypeMetadataService),
                    nameof(IWebHookBodyTypeMetadataService.BodyType),
                    receiverBodyTypeMetadata.BodyType,
                    receiverBodyTypeMetadata.ReceiverName);

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Shared_InvalidAttributeValue,
                    actionBodyTypeMetadata.GetType(),
                    typeof(IWebHookBodyTypeMetadata),
                    nameof(IWebHookBodyTypeMetadata.BodyType));
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Ensure given <paramref name="eventMetadata"/> is not <see langword="null"/>. An
        /// <see cref="IWebHookEventMetadata"/> service is mandatory for receivers with an attribute that implements
        /// <see cref="IWebHookEventSelectorMetadata"/>.
        /// </summary>
        /// <param name="eventMetadata">
        /// The <paramref name="receiverName"/> receiver's <see cref="IWebHookEventMetadata"/>, if any.
        /// </param>
        /// <param name="receiverName">The name of an available <see cref="IWebHookReceiver"/>.</param>
        protected void EnsureValidEventMetadata(IWebHookEventMetadata eventMetadata, string receiverName)
        {
            if (receiverName == null)
            {
                // Unusual case likely involves a GeneralWebHookAttribute subclass that implements
                // IWebHookEventSelectorMetadata. Assume developer adds runtime checks for IWebHookEventMetadata.
                return;
            }

            if (eventMetadata == null)
            {
                // IWebHookEventMetadata is mandatory when performing action selection using event names.
                _logger.LogCritical(
                    6,
                    "Invalid metadata services found for the '{ReceiverName}' WebHook receiver. Receivers with " +
                    "attributes implementing '{AttributeMetadataType}' must also provide a " +
                    "'{ServiceMetadataType}' service. Event selection is impossible otherwise.",
                    receiverName,
                    typeof(IWebHookEventSelectorMetadata),
                    typeof(IWebHookEventMetadata));

                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.MetadataProvider_MissingMetadataServices,
                    receiverName,
                    typeof(IWebHookEventSelectorMetadata),
                    typeof(IWebHookEventMetadata));
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Ensure members of given <paramref name="eventFromBodyMetadata"/> collection are valid. That is, confirm
        /// no receiver provides both <see cref="IWebHookEventFromBodyMetadata"/> and
        /// <see cref="IWebHookEventMetadata"/> services.
        /// </summary>
        /// <param name="eventFromBodyMetadata">
        /// The collection of <see cref="IWebHookEventFromBodyMetadata"/> services.
        /// </param>
        /// <param name="eventMetadata">
        /// The collection of <see cref="IWebHookEventMetadata"/> services.
        /// </param>
        protected void EnsureValidEventFromBodyMetadata(
            IReadOnlyList<IWebHookEventFromBodyMetadata> eventFromBodyMetadata,
            IReadOnlyList<IWebHookEventMetadata> eventMetadata)
        {
            if (eventFromBodyMetadata == null)
            {
                throw new ArgumentNullException(nameof(eventFromBodyMetadata));
            }
            if (eventMetadata == null)
            {
                throw new ArgumentNullException(nameof(eventMetadata));
            }

            var invalidMetadata = false;
            var receiversWithConflictingMetadata = eventFromBodyMetadata
                .Where(metadata => eventMetadata.Any(
                    innerMetadata => innerMetadata.IsApplicable(metadata.ReceiverName)))
                .Select(metadata => metadata.ReceiverName);
            foreach (var receiverName in receiversWithConflictingMetadata)
            {
                invalidMetadata = true;
                _logger.LogCritical(
                    7,
                    "Invalid metadata services found for the '{ReceiverName}' WebHook receiver. Receivers must not " +
                    "provide both '{EventFromBodyMetadataType}' and '{EventMetadataType}' services.",
                    receiverName,
                    typeof(IWebHookEventFromBodyMetadata),
                    typeof(IWebHookEventMetadata));
            }

            if (invalidMetadata)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.MetadataProvider_ConflictingMetadataServices,
                    typeof(IWebHookEventFromBodyMetadata),
                    typeof(IWebHookEventMetadata));
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// Ensure given <paramref name="services"/> collection does not contain duplicate registrations. That is,
        /// confirm the <typeparamref name="TService"/> registration for each
        /// <see cref="IWebHookReceiver.ReceiverName"/> is unique.
        /// </summary>
        /// <typeparam name="TService">
        /// The <see cref="IWebHookReceiver"/> interface of the <paramref name="services"/> to check.
        /// </typeparam>
        /// <param name="services">The collection of <typeparamref name="TService"/> services to check.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if duplicates exist in <paramref name="services"/>.
        /// </exception>
        protected void EnsureUniqueRegistrations<TService>(IReadOnlyList<TService> services)
            where TService : IWebHookReceiver
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var duplicateReceiverNames = services
                .GroupBy(item => item.ReceiverName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() != 1)
                .Select(group => group.Key);

            var hasDuplicates = false;
            foreach (var receiverName in duplicateReceiverNames)
            {
                hasDuplicates = true;
                _logger.LogCritical(
                    8,
                    "Duplicate '{MetadataType}' registrations found for the '{ReceiverName}' WebHook receiver.",
                    typeof(TService),
                    receiverName);
            }

            if (hasDuplicates)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.MetadataProvider_DuplicateMetadata,
                    typeof(TService));
                throw new InvalidOperationException(message);
            }
        }
    }
}
