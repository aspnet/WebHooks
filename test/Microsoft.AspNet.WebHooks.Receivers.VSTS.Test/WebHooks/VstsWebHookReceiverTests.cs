﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.WebHooks
{
    public class VstsWebHookReceiverTests : WebHookReceiverTestsBase<VstsWebHookReceiver>
    {
        private const string TestAction = "message.posted";
        private const string TestId = "";
        private const string TestSecret = "12345678901234567890123456789012";
        private const string TestAddress = "https://some.ssl.host?code=" + TestSecret;
        private HttpRequestMessage _postRequest;

        public static TheoryData<string> InvalidCodeQueries
        {
            get
            {
                return new TheoryData<string>
                {
                    string.Empty,
                    "=",
                    "==",
                    "invalid",
                    "code",
                    "code=",
                    "k1=v1;k2=v2",
                };
            }
        }

        [Fact]
        public void ReceiverName_IsConsistent()
        {
            // Arrange
            IWebHookReceiver rec = new VstsWebHookReceiver();
            string expected = "vsts";

            // Act
            string actual1 = rec.Name;
            string actual2 = VstsWebHookReceiver.ReceiverName;

            // Assert
            Assert.Equal(expected, actual1);
            Assert.Equal(actual1, actual2);
        }

        [Fact]
        public async Task ReceiveAsync_Throws_IfPostIsNotUsingHttps()
        {
            // Arrange
            Initialize(TestSecret);
            _postRequest.RequestUri = new Uri("http://some.no.ssl.host");

            // Act
            HttpResponseException ex = await Assert.ThrowsAsync<HttpResponseException>(() => ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _postRequest));

            // Assert
            HttpError error = await ex.Response.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The WebHook receiver 'VstsWebHookReceiverProxy' requires HTTPS in order to be secure. Please register a WebHook URI of type 'https'.", error.Message);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Theory]
        [MemberData("InvalidCodeQueries")]
        public async Task ReceiveAsync_Throws_IfPostHasNoCodeParameter(string query)
        {
            // Arrange
            Initialize(TestSecret);
            _postRequest.RequestUri = new Uri("https://some.no.ssl.host?" + query);

            // Act
            HttpResponseException ex = await Assert.ThrowsAsync<HttpResponseException>(() => ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _postRequest));

            // Assert
            HttpError error = await ex.Response.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The WebHook verification request must contain a 'code' query parameter.", error.Message);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Fact]
        public async Task ReceiveAsync_Throws_IfPostHasWrongCodeParameter()
        {
            // Arrange
            Initialize(TestSecret);
            _postRequest.RequestUri = new Uri("https://some.no.ssl.host?code=invalid");

            // Act
            HttpResponseException ex = await Assert.ThrowsAsync<HttpResponseException>(() => ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, _postRequest));

            // Assert
            HttpError error = await ex.Response.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The 'code' query parameter provided in the HTTP request did not match the expected value.", error.Message);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Theory]
        [MemberData("ValidIdData")]
        public async Task ReceiveAsync_Throws_IfPostIsNotJson(string id)
        {
            // Arrange
            Initialize(GetConfigValue(id, TestSecret));
            _postRequest.Content = new StringContent("Hello World!", Encoding.UTF8, "text/plain");

            // Act
            HttpResponseException ex = await Assert.ThrowsAsync<HttpResponseException>(() => ReceiverMock.Object.ReceiveAsync(id, RequestContext, _postRequest));

            // Assert
            HttpError error = await ex.Response.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The WebHook request must contain an entity body formatted as JSON.", error.Message);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), id, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Theory]
        [MemberData("ValidIdData")]
        public async Task ReceiveAsync_ReturnsError_IfPostHasNoEventTypeProperty(string id)
        {
            // Arrange
            Initialize(GetConfigValue(id, TestSecret));
            _postRequest.Content = CreateRequestContent("Microsoft.AspNet.WebHooks.Messages.bad.noEventType.json");

            // Act
            HttpResponseMessage actual = await ReceiverMock.Object.ReceiveAsync(id, RequestContext, _postRequest);

            // Assert
            HttpError error = await actual.Content.ReadAsAsync<HttpError>();
            Assert.Equal("No property 'eventType' was found in root of the object.", error.Message);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), id, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Theory]
        [MemberData("ValidIdData")]
        public async Task ReceiveAsync_Succeeds_IfValidPostRequest(string id)
        {
            // Arrange
            Initialize(GetConfigValue(id, TestSecret));
            List<string> actions = new List<string> { TestAction };
            ReceiverMock.Protected()
                .Setup<Task<HttpResponseMessage>>("ExecuteWebHookAsync", id, RequestContext, _postRequest, actions, ItExpr.IsAny<object>())
                .ReturnsAsync(new HttpResponseMessage())
                .Verifiable();

            // Act
            await ReceiverMock.Object.ReceiveAsync(id, RequestContext, _postRequest);

            // Assert
            ReceiverMock.Verify();
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PATCH")]
        [InlineData("PUT")]
        [InlineData("OPTIONS")]
        public async Task ReceiveAsync_ReturnsError_IfInvalidMethod(string method)
        {
            // Arrange
            Initialize(TestSecret);
            HttpRequestMessage req = new HttpRequestMessage { Method = new HttpMethod(method) };
            req.SetRequestContext(RequestContext);

            // Act
            HttpResponseMessage actual = await ReceiverMock.Object.ReceiveAsync(TestId, RequestContext, req);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, actual.StatusCode);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), TestId, RequestContext, req, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        public override void Initialize(string config)
        {
            base.Initialize(config);
            HttpConfig.InitializeReceiveVstsWebHooks();

            _postRequest = new HttpRequestMessage(HttpMethod.Post, TestAddress);
            _postRequest.SetRequestContext(RequestContext);
            _postRequest.Content = CreateRequestContent("Microsoft.AspNet.WebHooks.Messages.message.posted.json");
        }

        private HttpContent CreateRequestContent(string payload)
        {
            string content = EmbeddedResource.ReadAsString(payload);
            return new StringContent(content, Encoding.UTF8, "application/json");
        }
    }
}
