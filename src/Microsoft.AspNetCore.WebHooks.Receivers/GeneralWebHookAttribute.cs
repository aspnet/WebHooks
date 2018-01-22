// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.WebHooks.Metadata;
using Microsoft.AspNetCore.WebHooks.Properties;

namespace Microsoft.AspNetCore.WebHooks
{
    /// <summary>
    /// <para>
    /// An <see cref="Attribute"/> indicating the associated action is a WebHook endpoint for all enabled receivers.
    /// Specifies the expected <see cref="BodyType"/>, optional <see cref="EventName"/>, and optional
    /// <see cref="WebHookAttribute.Id"/>. Also adds a <see cref="Filters.WebHookReceiverExistsFilter"/> for the
    /// action.
    /// </para>
    /// <para>
    /// The signature of the action should be:
    /// <code>
    /// Task{IActionResult} ActionName(string receiverName, string id, string[] events, TData data)
    /// </code>
    /// or the subset of parameters required. <c>TData</c> must be compatible with expected requests and
    /// <see cref="BodyType"/>.
    /// </para>
    /// <para>
    /// An example WebHook URI is '<c>https://{host}/api/webhooks/incoming/{receiver name}/{id}</c>' or
    /// '<c>https://{host}/api/webhooks/incoming/{receiver name}/{id}?code=94c0c780e49a5c72972590571fd8</c>'.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the application enables CORS in general (see the <c>Microsoft.AspNetCore.Cors</c> package), apply
    /// <c>DisableCorsAttribute</c> to this action. If the application depends on the
    /// <c>Microsoft.AspNetCore.Mvc.ViewFeatures</c> package, apply <c>IgnoreAntiforgeryTokenAttribute</c> to this
    /// action.
    /// </para>
    /// <para>
    /// <see cref="GeneralWebHookAttribute"/> should be used at most once per <see cref="WebHookAttribute.Id"/> and
    /// <see cref="EventName"/> in a WebHook application.
    /// </para>
    /// </remarks>
    public class GeneralWebHookAttribute : WebHookAttribute, IWebHookBodyTypeMetadata, IWebHookEventSelectorMetadata
    {
        private WebHookBodyType _bodyType = WebHookBodyType.All;
        private string _eventName;

        /// <summary>
        /// Instantiates a new <see cref="GeneralWebHookAttribute"/> indicating the associated action is a WebHook
        /// endpoint for all enabled receivers.
        /// </summary>
        public GeneralWebHookAttribute()
            : base()
        {
        }

        /// <inheritdoc />
        /// <value>
        /// Default value is <see cref="WebHookBodyType.All"/>, indicating the action does not have body type
        /// requirements beyond those of the registered receivers. Should be set to a specific (single flag) value if
        /// the action has a <c>data</c> parameter.
        /// </value>
        public WebHookBodyType BodyType
        {
            get
            {
                return _bodyType;
            }
            set
            {
                // Avoid Enum.IsDefined because we want to distinguish invalid flag combinations from undefined flags.
                switch (value)
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
                        // 0 or contains an invalid combination of flags.
                        {
                            var message = string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.GeneralAttribute_InvalidBodyType,
                                value,
                                nameof(WebHookBodyType),
                                WebHookBodyType.All);
                            throw new ArgumentException(message, nameof(value));
                        }

                    default:
                        // Contains undefined flags.
                        {
                            var message = string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.General_InvalidEnumValue,
                                nameof(WebHookBodyType),
                                value);
                            throw new ArgumentException(message, nameof(value));
                        }
                }

                _bodyType = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the event the associated controller action accepts.
        /// </summary>
        /// <value>Default value is <see langword="null"/>, indicating this action accepts all events.</value>
        public string EventName
        {
            get
            {
                return _eventName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(Resources.General_ArgumentCannotBeNullOrEmpty, nameof(value));
                }

                _eventName = value;
            }
        }
    }
}
