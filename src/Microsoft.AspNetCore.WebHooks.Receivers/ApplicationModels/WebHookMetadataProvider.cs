// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.WebHooks.Metadata;

namespace Microsoft.AspNetCore.WebHooks.ApplicationModels
{
    // TODO: Add resources for exceptions thrown here.
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
        private readonly IReadOnlyList<IWebHookEventMetadata> _eventMetadata;
        private readonly IReadOnlyList<IWebHookRequestMetadataService> _requestMetadata;

        /// <summary>
        /// Instantiates a new <see cref="WebHookMetadataProvider"/> with the given <paramref name="metadata"/>.
        /// </summary>
        /// <param name="metadata">The collection of <see cref="IWebHookMetadata"/> services.</param>
        public WebHookMetadataProvider(IEnumerable<IWebHookMetadata> metadata)
        {
            _bindingMetadata = new List<IWebHookBindingMetadata>(metadata.OfType<IWebHookBindingMetadata>());
            _eventMetadata = new List<IWebHookEventMetadata>(metadata.OfType<IWebHookEventMetadata>());
            _requestMetadata = new List<IWebHookRequestMetadataService>(
                metadata.OfType<IWebHookRequestMetadataService>());

            // Check for duplicate IWebHookBindingMetadata registrations for a receiver.
            var bindingGroups = _bindingMetadata.GroupBy(item => item.ReceiverName, StringComparer.OrdinalIgnoreCase);
            foreach (var group in bindingGroups)
            {
                if (group.Count() != 1)
                {
                    var message = group.Key;
                    throw new InvalidOperationException(message);
                }
            }

            // Check for duplicate IWebHookEventMetadata registrations for a receiver. IWebHookEventMetadata is
            // optional in general because some receivers place event information in the request body.
            var eventGroups = _eventMetadata.GroupBy(item => item.ReceiverName, StringComparer.OrdinalIgnoreCase);
            foreach (var group in eventGroups)
            {
                if (group.Count() != 1)
                {
                    var message = group.Key;
                    throw new InvalidOperationException(message);
                }
            }

            // Check for IWebHookRequestMetadata services that do not also implement IWebHookReceiver.
            var nonReceivers = metadata
                .Where(item => item is IWebHookRequestMetadata && !(item is IWebHookRequestMetadataService));
            if (nonReceivers.Any())
            {
                var message = string.Join(", ", nonReceivers.Select(item => item.GetType().Name));
                throw new InvalidOperationException(message);
            }

            // Check for duplicate IWebHookRequestMetadataService registrations for a receiver.
            var requestGroups = _requestMetadata.GroupBy(item => item.ReceiverName, StringComparer.OrdinalIgnoreCase);
            foreach (var group in requestGroups)
            {
                if (group.Count() != 1)
                {
                    var message = group.Key;
                    throw new InvalidOperationException(message);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IApplicationModelProvider.Order"/> value used in all
        /// <see cref="WebHookMetadataProvider"/> instances.
        /// </summary>
        /// <value>
        /// Chosen to ensure this provider runs after MVC's <see cref="Mvc.Internal.DefaultApplicationModelProvider"/>.
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
            // Nothing to do.
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
                var bindingMMetadata = _bindingMetadata.FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (bindingMMetadata != null)
                {
                    action.Properties[typeof(IWebHookBindingMetadata)] = bindingMMetadata;
                }
            }

            IWebHookEventMetadata eventMetadata;
            if (receiverName == null)
            {
                // Pass along all IWebHookEventMetadata instances.
                eventMetadata = null;
                action.Properties[typeof(IWebHookEventMetadata)] = _eventMetadata;
            }
            else
            {
                eventMetadata = _eventMetadata.FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (eventMetadata != null)
                {
                    action.Properties[typeof(IWebHookEventMetadata)] = eventMetadata;
                }
            }

            if (attribute is IWebHookEventSelectorMetadata eventSelector &&
                eventSelector.EventName != null)
            {
                if (eventMetadata == null && receiverName != null)
                {
                    // IWebHookEventMetadata is mandatory when performing action selection using event names.
                    throw new InvalidOperationException();
                }

                action.Properties[typeof(IWebHookEventSelectorMetadata)] = eventSelector;
            }

            // Find the request metadata. IWebHookRequestMetadata is mandatory for every receiver.
            if (!(attribute is IWebHookRequestMetadata requestMetadata))
            {
                if (receiverName == null)
                {
                    // Only the GeneralWebHookAttribute should have a null ReceiverName and it implements
                    // IWebHookRequestMetadata.
                    throw new InvalidOperationException();
                }

                requestMetadata = _requestMetadata.FirstOrDefault(metadata => metadata.IsApplicable(receiverName));
                if (requestMetadata == null)
                {
                    throw new InvalidOperationException();
                }
            }

            action.Properties[typeof(IWebHookRequestMetadata)] = requestMetadata;
        }
    }
}
