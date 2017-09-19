// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Routing;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.WebHooks.ApplicationModels
{
    /// <summary>
    /// An <see cref="IApplicationModelProvider"/> implementation that adds <see cref="IBindingSourceMetadata"/> and
    /// <see cref="IModelNameProvider"/> information to <see cref="ParameterModel"/>s of WebHook actions.
    /// </summary>
    public class WebHookModelBindingProvider : IApplicationModelProvider
    {
        /// <inheritdoc />
        public int Order => WebHookMetadataProvider.Order + 20;

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
                    var attribute = action.Attributes.OfType<WebHookAttribute>().FirstOrDefault();
                    if (attribute == null)
                    {
                        // Not a WebHook handler.
                        continue;
                    }

                    for (var k = 0; k < action.Parameters.Count; k++)
                    {
                        var parameter = action.Parameters[k];
                        Apply(parameter);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            // Nothing to do.
        }

        private static void Apply(ParameterModel parameter)
        {
            var bindingInfo = parameter.BindingInfo;
            if (bindingInfo?.BinderModelName != null ||
                bindingInfo?.BinderType != null ||
                bindingInfo?.BindingSource != null)
            {
                // User was explicit. Nothing to do.
                return;
            }

            if (bindingInfo == null)
            {
                bindingInfo = parameter.BindingInfo = new BindingInfo();
            }

            var parameterType = parameter.ParameterInfo.ParameterType;
            switch (parameter.ParameterName.ToUpperInvariant())
            {
                case "ACTION":
                case "ACTIONS":
                case "ACTIONNAME":
                case "ACTIONNAMES":
                    SourceEvent(bindingInfo, parameterType);
                    break;

                case "DATA":
                    SourceData(bindingInfo, parameter.Action);
                    break;

                case "EVENT":
                case "EVENTS":
                case "EVENTNAME":
                case "EVENTNAMES":
                    SourceEvent(bindingInfo, parameterType);
                    break;

                case "ID":
                    SourceId(bindingInfo, parameterType);
                    break;

                case "RECEIVER":
                case "RECEIVERNAME":
                    SourceReceiver(bindingInfo, parameterType);
                    break;

                case "RECEIVERID":
                    SourceId(bindingInfo, parameterType);
                    break;

                case "WEBHOOKRECEIVER":
                    SourceReceiver(bindingInfo, parameterType);
                    break;

                default:
                    // ??? Should we support NameValueCollection here and in model binding to ease migration from
                    // ??? current WebHooks?
                    // ??? Any need to support simple JToken's? JContainer is the base for JArray and JObject.
                    // Regardless of name, treat all IFormCollection, JObject, and XElement parameters as data.
                    if (typeof(IFormCollection).IsAssignableFrom(parameterType) ||
                        typeof(JContainer).IsAssignableFrom(parameterType) ||
                        typeof(XElement).IsAssignableFrom(parameterType))
                    {
                        SourceData(bindingInfo, parameter.Action);
                    }
                    break;
            }
        }

        private static void SourceData(BindingInfo bindingInfo, ActionModel action)
        {
            if (action.Properties.TryGetValue(typeof(IWebHookRequestMetadata), out var metadata))
            {
                var requestMetadata = (IWebHookRequestMetadata)metadata;
                if (requestMetadata.BodyType == WebHookBodyType.Form)
                {
                    // ??? Should we instead support multiple parameters binding to portions of the form data i.e.
                    // ??? leave BinderModelName null and let the parameter name bleed through into model binding?
                    bindingInfo.BinderModelName = string.Empty;
                    bindingInfo.BindingSource = BindingSource.Form;
                }
                else
                {
                    bindingInfo.BinderModelName = string.Empty;
                    bindingInfo.BindingSource = BindingSource.Body;
                }
            }
        }

        private static void SourceEvent(BindingInfo bindingInfo, Type parameterType)
        {
            if (typeof(string) != parameterType &&
                !typeof(IEnumerable<string>).IsAssignableFrom(parameterType))
            {
                // ??? Do we need logging about these strange cases?
                // Unexpected / unsupported type. Do nothing.
                return;
            }

            bindingInfo.BinderModelName = WebHookReceiverRouteNames.EventKeyName;
            bindingInfo.BindingSource = BindingSource.Path;
        }

        private static void SourceId(BindingInfo bindingInfo, Type parameterType)
        {
            if (typeof(string) != parameterType)
            {
                // Unexpected / unsupported type. Do nothing.
                return;
            }

            bindingInfo.BinderModelName = WebHookReceiverRouteNames.IdKeyName;
            bindingInfo.BindingSource = BindingSource.Path;
        }

        private static void SourceReceiver(BindingInfo bindingInfo, Type parameterType)
        {
            if (typeof(string) != parameterType)
            {
                // Unexpected / unsupported type. Do nothing.
                return;
            }

            bindingInfo.BinderModelName = WebHookReceiverRouteNames.ReceiverKeyName;
            bindingInfo.BindingSource = BindingSource.Path;
        }
    }
}
