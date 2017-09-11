// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.WebHooks.Filters;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;
using Microsoft.AspNetCore.WebHooks.Routing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebHooks.ApplicationModels
{
    /// <summary>
    /// An <see cref="IActionModelConvention"/> implementation that adds attribute routing information to WebHook
    /// actions.
    /// </summary>
    public class WebHookRoutingProvider : IApplicationModelProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Instantiates a new <see cref="WebHookRoutingProvider"/> with the given <paramref name="loggerFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public WebHookRoutingProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc />
        public int Order => WebHookMetadataProvider.Order + 10;

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

        /// <inheritdoc />
        public void Apply(ActionModel action)
        {
            var attribute = action.Attributes.OfType<WebHookActionAttributeBase>().FirstOrDefault();
            if (attribute == null)
            {
                // Not a WebHook handler.
                return;
            }

            var routeValues = action.RouteValues;
            AddRouteValues(attribute, routeValues);

            var template = ChooseTemplate(routeValues);
            var selectors = action.Selectors;
            if (selectors.Count == 0)
            {
                var selector = new SelectorModel();
                selectors.Add(selector);

                AddTemplate(attribute, template, selector);
            }
            else
            {
                for (var i = 0; i < selectors.Count; i++)
                {
                    var selector = selectors[i];
                    AddTemplate(attribute, template, selector);
                }
            }

            AddConstraints(action.Properties, selectors);

            if (action.Properties.TryGetValue(typeof(IWebHookRequestMetadata), out var requestMetadata))
            {
                action.Filters.Add(new WebHookVerifyBodyTypeFilter(
                    _loggerFactory,
                    (IWebHookRequestMetadata)requestMetadata));
            }
        }

        // Add specified route values to constrain the route. Similar to 2 or 3 IRouteValueProvider attributes.
        private static void AddRouteValues(
            WebHookActionAttributeBase attribute,
            IDictionary<string, string> routeValues)
        {
            if (attribute.ReceiverName != null &&
                !routeValues.ContainsKey(WebHookReceiverRouteNames.ReceiverKeyName))
            {
                routeValues.Add(WebHookReceiverRouteNames.ReceiverKeyName, attribute.ReceiverName);
            }

            if (attribute.Id != null &&
                !routeValues.ContainsKey(WebHookReceiverRouteNames.IdKeyName))
            {
                routeValues.Add(WebHookReceiverRouteNames.IdKeyName, attribute.Id);
            }

            if (attribute is IWebHookEventSelectorMetadata eventSelector &&
                eventSelector.EventName != null &&
                !routeValues.ContainsKey(WebHookReceiverRouteNames.EventKeyName))
            {
                routeValues.Add(WebHookReceiverRouteNames.EventKeyName, eventSelector.EventName);
            }
        }

        private static string ChooseTemplate(IDictionary<string, string> routeValues)
        {
            var template = "/api/webhooks/incoming/";
            if (routeValues.ContainsKey(WebHookReceiverRouteNames.ReceiverKeyName))
            {
                template += $"[{WebHookReceiverRouteNames.ReceiverKeyName}]/";
            }
            else
            {
                template += $"{{{WebHookReceiverRouteNames.ReceiverKeyName}}}/";
            }

            if (routeValues.ContainsKey(WebHookReceiverRouteNames.IdKeyName))
            {
                template += $"[[{WebHookReceiverRouteNames.IdKeyName}]";
            }
            else
            {
                template += $"{{{WebHookReceiverRouteNames.IdKeyName}?}}";
            }

            return template;
        }

        // Set the template for given SelectorModel. Similar to WebHookActionAttributeBase implementing
        // IRouteTemplateProvider.
        private static void AddTemplate(
            WebHookActionAttributeBase attribute,
            string template,
            SelectorModel selector)
        {
            if (selector.AttributeRouteModel?.Template != null)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.RoutingConvention_MixedRouteWithWebHookAction,
                    attribute.GetType().Name,
                    selector.AttributeRouteModel.Attribute?.GetType().Name);
                throw new InvalidOperationException(message);
            }

            if (selector.AttributeRouteModel == null)
            {
                selector.AttributeRouteModel = new AttributeRouteModel();
            }

            selector.AttributeRouteModel.Template = template;
        }

        private static void AddConstraint(IActionConstraintMetadata constraint, IList<SelectorModel> selectors)
        {
            for (var i = 0; i < selectors.Count; i++)
            {
                var selector = selectors[i];
                selector.ActionConstraints.Add(constraint);
            }
        }

        private void AddConstraints(IDictionary<object, object> properties, IList<SelectorModel> selectors)
        {
            if (properties.TryGetValue(typeof(IWebHookEventMetadata), out var eventMetadata))
            {
                IActionConstraintMetadata constraint;
                if (eventMetadata is IWebHookEventMetadata singleEventMetadata)
                {
                    constraint = new WebHookSingleEventMapperConstraint(_loggerFactory, singleEventMetadata);
                }
                else
                {
                    constraint = new WebHookMultipleEventMapperConstraintFactory();
                }

                AddConstraint(constraint, selectors);
            }

            if (properties.TryGetValue(typeof(IWebHookEventSelectorMetadata), out var eventSourceMetadata))
            {
                var eventName = ((IWebHookEventSelectorMetadata)eventSourceMetadata).EventName;
                if (eventName != null)
                {
                    IActionConstraintMetadata constraint;
                    if (eventMetadata == null)
                    {
                        constraint = new WebHookSingleEventSelectorConstraint(eventName, pingEventName: null);
                    }
                    else if (eventMetadata is IWebHookEventMetadata singleEventMetadata)
                    {
                        constraint = new WebHookSingleEventSelectorConstraint(
                            eventName,
                            singleEventMetadata.PingEventName);
                    }
                    else
                    {
                        constraint = new WebHookMultipleEventSelectorConstraint(
                            eventName,
                            (IEnumerable<IWebHookEventMetadata>)eventMetadata);
                    }

                    AddConstraint(constraint, selectors);
                }
            }
        }
    }
}
