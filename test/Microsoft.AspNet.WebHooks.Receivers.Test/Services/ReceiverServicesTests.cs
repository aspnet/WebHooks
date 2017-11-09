﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Web.Http;
using Microsoft.AspNet.WebHooks.Config;
using Microsoft.AspNet.WebHooks.Diagnostics;
using Moq;
using Xunit;

namespace Microsoft.AspNet.WebHooks.Services
{
    [Collection("ConfigCollection")]
    public class ReceiverServicesTests
    {
        public ReceiverServicesTests()
        {
            HttpConfiguration config = new HttpConfiguration();
            WebHooksConfig.Initialize(config);
            ReceiverServices.Reset();
        }

        [Fact]
        public void GetReceiverManager_ReturnsSingleInstance()
        {
            // Arrange
            ILogger logger = CommonServices.GetLogger();
            List<IWebHookReceiver> receivers = new List<IWebHookReceiver>();

            // Act
            IWebHookReceiverManager actual1 = ReceiverServices.GetReceiverManager(receivers, logger);
            IWebHookReceiverManager actual2 = ReceiverServices.GetReceiverManager(receivers, logger);

            // Assert
            Assert.Same(actual1, actual2);
        }

        [Fact]
        public void GetReceiverConfig_ReturnsSingleInstance()
        {
            // Arrange
            SettingsDictionary settings = CommonServices.GetSettings();
            ILogger logger = CommonServices.GetLogger();

            // Act
            IWebHookReceiverConfig actual1 = ReceiverServices.GetReceiverConfig(settings, logger);
            IWebHookReceiverConfig actual2 = ReceiverServices.GetReceiverConfig(settings, logger);

            // Assert
            Assert.Same(actual1, actual2);
        }

        [Fact]
        public void SetReceiverConfig_GetReceiverConfig_Roundtrips()
        {
            // Arrange
            SettingsDictionary settings = CommonServices.GetSettings();
            ILogger logger = CommonServices.GetLogger();
            Mock<IWebHookReceiverConfig> configMock = new Mock<IWebHookReceiverConfig>();

            // Act
            ReceiverServices.SetReceiverConfig(configMock.Object);
            IWebHookReceiverConfig actual = ReceiverServices.GetReceiverConfig(settings, logger);

            // Assert
            Assert.Same(configMock.Object, actual);
        }

        [Fact]
        public void GetHandlerSorter_ReturnsSingletonInstance()
        {
            // Act
            IWebHookHandlerSorter actual1 = ReceiverServices.GetHandlerSorter();
            IWebHookHandlerSorter actual2 = ReceiverServices.GetHandlerSorter();

            // Assert
            Assert.Same(actual1, actual2);
        }

        [Fact]
        public void GetReceivers_ReturnsSingletonInstance()
        {
            // Act
            IEnumerable<IWebHookReceiver> actual1 = ReceiverServices.GetReceivers();
            IEnumerable<IWebHookReceiver> actual2 = ReceiverServices.GetReceivers();

            // Assert
            Assert.Same(actual1, actual2);
        }

        [Fact]
        public void GetHandlers_ReturnsSingleInstance()
        {
            // Act
            IEnumerable<IWebHookHandler> actual1 = ReceiverServices.GetHandlers();
            IEnumerable<IWebHookHandler> actual2 = ReceiverServices.GetHandlers();

            // Assert
            Assert.Same(actual1, actual2);
        }
    }
}
