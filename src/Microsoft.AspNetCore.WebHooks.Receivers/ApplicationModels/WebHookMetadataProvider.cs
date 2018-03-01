// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;

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
        public WebHookMetadataProvider(
            IEnumerable<IWebHookBindingMetadata> bindingMetadata,
            IEnumerable<IWebHookBodyTypeMetadataService> bodyTypeMetadata,
            IEnumerable<IWebHookEventFromBodyMetadata> eventFromBodyMetadata,
            IEnumerable<IWebHookEventMetadata> eventMetadata,
            IEnumerable<IWebHookGetHeadRequestMetadata> getHeadRequestMetadata,
            IEnumerable<IWebHookPingRequestMetadata> pingRequestMetadata,
            IEnumerable<IWebHookVerifyCodeMetadata> verifyCodeMetadata)
        {
            _bindingMetadata = bindingMetadata.ToArray();
            _bodyTypeMetadata = bodyTypeMetadata.ToArray();
            _eventMetadata = eventMetadata.ToArray();
            _pingRequestMetadata = pingRequestMetadata.ToArray();

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
                action.Properties[typeof(IWebHookBodyTypeMetadataService)] = receiverBodyTypeMetadata;
            }

            // Ignore metadata if attribute has AllBodyTypes.
            if (attribute is IWebHookBodyTypeMetadata actionBodyTypeMetadata &&
                actionBodyTypeMetadata.BodyType != WebHookConstants.AllBodyTypes)
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

            foreach (var receiverBodyTypeMetadata in bodyTypeMetadata)
            {
                // Confirm the receiver's BodyType is valid.
                if (receiverBodyTypeMetadata.BodyType == 0)
                {
                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.MetadataProvider_InvalidMetadataServiceBodyType,
                        receiverBodyTypeMetadata.ReceiverName,
                        typeof(IWebHookBodyTypeMetadataService),
                        nameof(IWebHookBodyTypeMetadataService.BodyType));
                    throw new InvalidOperationException(message);
                }
                else if ((~WebHookConstants.AllBodyTypes & receiverBodyTypeMetadata.BodyType) != 0)
                {
                    // Value contains undefined flags.
                    var message = string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.General_InvalidEnumValue,
                        typeof(WebHookBodyType),
                        receiverBodyTypeMetadata.BodyType);
                    throw new InvalidOperationException(message);
                }
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
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Shared_MissingMetadata,
                    receiverName,
                    typeof(IWebHookBodyTypeMetadataService));
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
            // distinguish invalid flag combinations from undefined flags. This method is not called if
            // actionBodyTypeMetadata.BodyType is AllBodyTypes.
            switch (actionBodyTypeMetadata.BodyType)
            {
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
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.MetadataProvider_InvalidAttributeBodyType,
                            actionBodyTypeMetadata.GetType(),
                            typeof(IWebHookBodyTypeMetadata),
                            nameof(IWebHookBodyTypeMetadata.BodyType),
                            actionBodyTypeMetadata.BodyType,
                            WebHookConstants.AllBodyTypes,
                            nameof(WebHookBodyType));
                        throw new InvalidOperationException(message);
                    }

                default:
                    {
                        // Value contains undefined flags.
                        var message = string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.General_InvalidEnumValue,
                            typeof(WebHookBodyType),
                            actionBodyTypeMetadata.BodyType);
                        throw new InvalidOperationException(message);
                    }
            }

            // Attribute must require the same body type as receiver's metadata service or a subset. That is,
            // actionBodyTypeMetadata.BodyType flags must not include any beyond those set in
            // receiverBodyTypeMetadata.BodyType.
            if (receiverBodyTypeMetadata != null &&
                (~receiverBodyTypeMetadata.BodyType & actionBodyTypeMetadata.BodyType) != 0)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.Shared_NonSubsetAttributeBodyType,
                    actionBodyTypeMetadata.GetType(),
                    typeof(IWebHookBodyTypeMetadata),
                    nameof(IWebHookBodyTypeMetadata.BodyType),
                    actionBodyTypeMetadata.BodyType,
                    WebHookConstants.AllBodyTypes,
                    receiverBodyTypeMetadata.BodyType);
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

            var receiverWithConflictingMetadata = eventFromBodyMetadata
                .Where(metadata => eventMetadata.Any(
                    innerMetadata => innerMetadata.IsApplicable(metadata.ReceiverName)))
                .FirstOrDefault();
            if (receiverWithConflictingMetadata != null)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.MetadataProvider_ConflictingMetadataServices,
                    receiverWithConflictingMetadata.ReceiverName,
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

            var duplicateMetadataGroup = services
                .GroupBy(item => item.ReceiverName, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() != 1)
                .FirstOrDefault();
            if (duplicateMetadataGroup != null)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.MetadataProvider_DuplicateMetadata,
                    duplicateMetadataGroup.Key, // ReceiverName
                    typeof(TService));
                throw new InvalidOperationException(message);
            }
        }
    }
}
