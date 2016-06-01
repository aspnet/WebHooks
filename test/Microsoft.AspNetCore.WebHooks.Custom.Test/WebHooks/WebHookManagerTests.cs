﻿using Microsoft.Extensions.Logging;
using Microsoft.NetCore.TestUtilities.Mocks;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace Microsoft.AspNetCore.WebHooks.WebHooks
{
    public class WebHookManagerTests : IDisposable
    {
        private const string TestUser = "TestUser";

        private readonly HttpClient _httpClient;
        private readonly Mock<IWebHookStore> _storeMock;
        private readonly Mock<IWebHookSender> _senderMock;
        private readonly HttpMessageHandlerMock _handlerMock;
        private readonly Mock<ILogger> _loggerMock;
        private readonly HttpResponseMessage _response;

        private WebHookManager _manager;

        public WebHookManagerTests()
        {
            _handlerMock = new HttpMessageHandlerMock();
            _httpClient = new HttpClient(_handlerMock);
            _storeMock = new Mock<IWebHookStore>();
            _storeMock.Setup<Task<ICollection<WebHook>>>(s => s.QueryWebHooksAsync(TestUser, new[] { "a1" }, null))
                .ReturnsAsync(new Collection<WebHook> { CreateWebHook() })
                .Verifiable();
            _senderMock = new Mock<IWebHookSender>();
            _loggerMock = new Mock<ILogger>();
            _response = new HttpResponseMessage();
        }

        public static TheoryData<string> WebHookSecretData
        {
            get
            {
                return new TheoryData<string>
                {
                    null,
                    string.Empty,
                    new string('a', 31),
                    new string('a', 65),
                };
            }
        }

        public static TheoryData<string> WebHookUriData
        {
            get
            {
                return new TheoryData<string>
                {
                    null,
                    "ftp://localhost",
                    "telnet://localhost",
                    "htp://localhost",
                    "invalid://localhost",
                };
            }
        }

        public static TheoryData<IEnumerable<WebHook>, IWebHookNotification> FilterSingleNotificationData
        {
            get
            {
                return new TheoryData<IEnumerable<WebHook>, IWebHookNotification>
                {
                    { new[] { CreateWebHook("_") }, CreateNotification("a") },
                    { new[] { CreateWebHook("a") }, CreateNotification("a") },
                    { new[] { CreateWebHook("*") }, CreateNotification("a") },

                    { new[] { CreateWebHook("_"), CreateWebHook("a") }, CreateNotification("a") },
                    { new[] { CreateWebHook("a"), CreateWebHook("_") }, CreateNotification("a") },
                    { new[] { CreateWebHook("*"), CreateWebHook("_") }, CreateNotification("a") },
                    { new[] { CreateWebHook("*"), CreateWebHook("*") }, CreateNotification("a") },
                };
            }
        }

        public static TheoryData<IEnumerable<WebHook>, IEnumerable<IWebHookNotification>, int> FilterMultipleNotificationData
        {
            get
            {
                return new TheoryData<IEnumerable<WebHook>, IEnumerable<IWebHookNotification>, int>
                {
                    { new WebHook[0], new IWebHookNotification[0], 0 },

                    { new[] { CreateWebHook("_") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 0 },
                    { new[] { CreateWebHook("a") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 1 },
                    { new[] { CreateWebHook("b") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 1 },
                    { new[] { CreateWebHook("c") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 1 },
                    { new[] { CreateWebHook("*") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 1 },

                    { new[] { CreateWebHook("_"), CreateWebHook("_"), CreateWebHook("_") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 0 },
                    { new[] { CreateWebHook("_"), CreateWebHook("a"), CreateWebHook("_") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 1 },
                    { new[] { CreateWebHook("a"), CreateWebHook("_"), CreateWebHook("a") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 2 },
                    { new[] { CreateWebHook("a"), CreateWebHook("a"), CreateWebHook("a") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 3 },
                    { new[] { CreateWebHook("*"), CreateWebHook("a"), CreateWebHook("*") }, new[] { CreateNotification("a"), CreateNotification("b"), CreateNotification("c") }, 3 },
                };
            }
        }

        [Theory]
        [MemberData("WebHookSecretData")]
        public async Task VerifyWebHookAsync_Throws_IfInvalidWebHookSecret(string secret)
        {
            // Arrange
            _manager = new WebHookManager(_storeMock.Object, _senderMock.Object, _loggerMock.Object, _httpClient);
            WebHook webHook = CreateWebHook();
            webHook.Secret = secret;

            // Act
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.VerifyWebHookAsync(webHook));

            // Assert
            Assert.Equal("The WebHook secret key parameter must be between 32 and 64 characters long.", ex.Message);
        }


        [Theory]
        [MemberData("WebHookUriData")]
        public async Task VerifyWebHookAsync_Throws_IfNotHttpOrHttpsUri(string webHookUri)
        {


            // Arrange
            _manager = new WebHookManager(_storeMock.Object, _senderMock.Object, _loggerMock.Object, _httpClient);
            WebHook webHook = CreateWebHook();
            webHook.WebHookUri = webHookUri != null ? new Uri(webHookUri) : null;

            // Act
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.VerifyWebHookAsync(webHook));

            // Assert
            Assert.Equal(string.Format("The WebHook URI must be absolute with a scheme of either 'http' or 'https' but received '{0}'.", webHook.WebHookUri), ex.Message);
        }

        [Fact]
        public async Task VerifyWebHookAsync_Throws_IfHttpClientThrows()
        {
            // Arrange
            _manager = new WebHookManager(_storeMock.Object, _senderMock.Object, _loggerMock.Object, _httpClient);
            _handlerMock.Handler = (req, counter) => { throw new Exception("Catch this!"); };
            WebHook webHook = CreateWebHook();

            // Act
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.VerifyWebHookAsync(webHook));

            // Assert
            Assert.Equal("WebHook verification failed. Please ensure that the WebHook URI is valid and that the endpoint is accessible. Error encountered: Catch this!", ex.Message);
        }

        [Fact]
        public async Task VerifyWebHookAsync_Throws_IEmptySuccessResponse()
        {
            // Arrange
            _manager = new WebHookManager(_storeMock.Object, _senderMock.Object, _loggerMock.Object, _httpClient);
            _handlerMock.Handler = (req, counter) => Task.FromResult(_response);
            WebHook webHook = CreateWebHook();

            // Act
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.VerifyWebHookAsync(webHook));

            // Assert
            Assert.Equal("The WebHook URI did not return the expected echo query parameter value in a plain text response body. This is necessary to ensure that the WebHook is connected correctly.", ex.Message);
        }

        [Fact]
        public async Task VerifyWebHookAsync_Throws_INotSuccessResponse()
        {
            // Arrange
            _response.StatusCode = HttpStatusCode.NotFound;
            _manager = new WebHookManager(_storeMock.Object, _senderMock.Object, _loggerMock.Object, _httpClient);
            _handlerMock.Handler = (req, counter) => Task.FromResult(_response);
            WebHook webHook = CreateWebHook();

            // Act
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.VerifyWebHookAsync(webHook));

            // Assert
            Assert.Equal("WebHook verification failed. Please ensure that the WebHook URI is valid and that the endpoint is accessible. Error encountered: NotFound", ex.Message);
        }

        [Fact]
        public async Task VerifyWebHookAsync_Throws_IfEchoDoesNotMatch()
        {
            // Arrange
            _response.Content = new StringContent("Hello World");
            _manager = new WebHookManager(_storeMock.Object, _senderMock.Object, _loggerMock.Object, _httpClient);
            _handlerMock.Handler = (req, counter) => Task.FromResult(_response);
            WebHook webHook = CreateWebHook();

            // Act
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _manager.VerifyWebHookAsync(webHook));

            // Assert
            Assert.Equal("The HTTP request echo query parameter was not returned as plain text in the response. Please return the echo parameter to verify that the WebHook is working as expected.", ex.Message);
        }

        [Fact]
        public async Task VerifyWebHookAsync_Succeeds_EchoResponse()
        {
            // Arrange
            _manager = new WebHookManager(_storeMock.Object, _senderMock.Object, _loggerMock.Object, _httpClient);
            _handlerMock.Handler = (req, counter) =>
            {
                Dictionary<string, StringValues> query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(req.RequestUri.Query);
                _response.Content = new StringContent(query["echo"]);
                return Task.FromResult(_response);
            };
            WebHook webHook = CreateWebHook();

            // Act
            await _manager.VerifyWebHookAsync(webHook);
        }

        [Theory]
        [MemberData("FilterSingleNotificationData")]
        public void GetWorkItems_FilterSingleNotification(IEnumerable<WebHook> webHooks, IWebHookNotification notification)
        {
            // Act
            IEnumerable<WebHookWorkItem> actual = WebHookManager.GetWorkItems(webHooks.ToArray(), new[] { notification });

            // Assert
            Assert.Equal(webHooks.Count(), actual.Count());
            foreach (WebHookWorkItem workItem in actual)
            {
                Assert.Same(workItem.Notifications.Single(), notification);
            }
        }

        [Theory]
        [MemberData("FilterMultipleNotificationData")]
        public void GetWorkItems_FilterMultipleNotifications(IEnumerable<WebHook> webHooks, IEnumerable<IWebHookNotification> notifications, int expected)
        {
            // Act
            IEnumerable<WebHookWorkItem> actual = WebHookManager.GetWorkItems(webHooks.ToArray(), notifications.ToArray());

            // Assert
            Assert.Equal(expected, actual.Count());
            foreach (WebHookWorkItem workItem in actual)
            {
                foreach (IWebHookNotification notification in workItem.Notifications)
                {
                    Assert.True(workItem.WebHook.MatchesAction(notification.Action));
                }
            }
        }

        public void Dispose()
        {
            if (_manager != null)
            {
                _manager.Dispose();
            }
            if (_handlerMock != null)
            {
                _handlerMock.Dispose();
            }
            if (_httpClient != null)
            {
                _httpClient.Dispose();
            }
            if (_response != null)
            {
                _response.Dispose();
            }
        }

        internal static WebHook CreateWebHook()
        {
            return CreateWebHook("a1");
        }

        internal static WebHook CreateWebHook(params string[] filters)
        {
            WebHook hook = new WebHook
            {
                Id = "1234",
                Description = "你好世界",
                Secret = "123456789012345678901234567890123456789012345678",
                WebHookUri = new Uri("http://localhost/hook"),
            };
            hook.Headers.Add("h1", "hv1");
            hook.Properties.Add("p1", "pv1");
            foreach (string filter in filters)
            {
                hook.Filters.Add(filter);
            }
            return hook;
        }

        private static IWebHookNotification CreateNotification(string action)
        {
            return new WebHookDataNotification(action, data: null);
        }
    }
}
