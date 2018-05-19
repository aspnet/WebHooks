// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.WebHooks.Sender.Test;
using Xunit;

namespace Microsoft.AspNetCore.WebHooks.WebHooks
{
    public class WebHookWorkItemTests
    {
        private readonly WebHookWorkItem _workItem;
        private readonly WebHook _webHook;
        private readonly Notification _notification;
        private readonly IEnumerable<Notification> _notifications;

        public WebHookWorkItemTests()
        {
            _notification = new Notification("action", payload: null);
            _notifications = new List<Notification> { _notification };
            _webHook = new WebHook();
            _workItem = new WebHookWorkItem(_webHook, _notifications.First());
        }

        [Fact]
        public void Id_Roundtrips()
        {
            PropertyAssert.Roundtrips(_workItem, s => s.Id, PropertySetter.NullDoesNotRoundtrip, roundtripValue: "value");
        }

        [Fact]
        public void WebHook_Roundtrips()
        {
            PropertyAssert.Roundtrips(_workItem, s => s.WebHook, PropertySetter.NullRoundtrips, defaultValue: _webHook, roundtripValue: new WebHook());
        }

        [Fact]
        public void Offset_Roundtrips()
        {
            _workItem.Offset = 1024;
            PropertyAssert.Roundtrips(_workItem, s => s.Offset, defaultValue: 1024, roundtripValue: 2048);
        }

        [Fact]
        public void Notifications_Initializes()
        {
            Assert.Equal(_notifications.First(), _workItem.Notification);
        }
    }
}
