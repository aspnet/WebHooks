// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.WebHooks.Properties;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.AspNet.WebHooks
{
    public class MyGetWebHookReceiverTests : WebHookReceiverTestsBase<MyGetWebHookReceiver>
    {
        private const string EmptyConfig = "";

        private const string TestContent = "{ \"key\": \"value\" }";
        private const string TestId = "B9834DCA-2E6B-4EFD-BE9F-B19A4B70A409";

        private HttpRequestMessage _postRequest;
        
        [SuppressMessage("Microsoft.Security.Cryptography", "CA5354:SHA1CannotBeUsed", Justification = "Required by GitHib")]
        public static TheoryData<string> ValidPostRequest
        {
            get
            {
                return new TheoryData<string>
                {
                    string.Empty,
                    TestId
                };
            }
        }

        [Theory]
        [MemberData("ValidPostRequest")]
        public async Task ReceiveAsync_Throws_IfPostIsNotJson(string id)
        {
            // Arrange
            Initialize(EmptyConfig);
            _postRequest.Content = new StringContent(TestContent, Encoding.UTF8, "text/plain");

            // Act
            HttpResponseException ex = await Assert.ThrowsAsync<HttpResponseException>(() => ReceiverMock.Object.ReceiveAsync(id, RequestContext, _postRequest));

            // Assert
            HttpError error = await ex.Response.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The WebHook request must contain an entity body formatted as JSON.", error.Message);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), id, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Theory]
        [MemberData("ValidPostRequest")]
        public async Task ReceiveAsync_ReturnsError_IfPostHasNoPayloadTypeProperty(string id)
        {
            // Arrange
            Initialize(EmptyConfig);

            // Act
            HttpResponseMessage actual = await ReceiverMock.Object.ReceiveAsync(id, RequestContext, _postRequest);

            // Assert
            HttpError error = await actual.Content.ReadAsAsync<HttpError>();
            Assert.Equal("The WebHook request must contain a 'PayloadType' JSON property indicating the type of event.", error.Message);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), id, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
        }

        [Theory]
        [MemberData("ValidPostRequest")]
        public async Task ReceiveAsync_Succeeds_IfValidPostRequest(string id)
        {
            // Arrange
            Initialize(EmptyConfig);
            List<string> actions = new List<string> { "action1" };
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
        [MemberData("ValidPostRequest")]
        public async Task ReceiveAsync_Succeeds_IfPostPing(string id)
        {
            // Arrange
            Initialize(EmptyConfig);
            _postRequest.Content = new StringContent(MyGetPayloads.PingPayload);

            // Act
            HttpResponseMessage response = await ReceiverMock.Object.ReceiveAsync(id, RequestContext, _postRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ReceiverMock.Protected()
                .Verify<Task<HttpResponseMessage>>("ExecuteWebHookAsync", Times.Never(), id, RequestContext, _postRequest, ItExpr.IsAny<IEnumerable<string>>(), ItExpr.IsAny<object>());
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
            Initialize(EmptyConfig);
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

            _postRequest = new HttpRequestMessage() { Method = HttpMethod.Post };
            _postRequest.SetRequestContext(RequestContext);
            _postRequest.Content = new StringContent(TestContent, Encoding.UTF8, "application/vnd.myget.webhooks.v1+json");
        }
    }
}
