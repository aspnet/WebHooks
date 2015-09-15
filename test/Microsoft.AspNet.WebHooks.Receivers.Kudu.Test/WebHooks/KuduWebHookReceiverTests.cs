// Copyright (c) .NET Foundation. All rights reserved.
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
    public class KuduWebHookReceiverTests : WebHookReceiverTestsBase<KuduWebHookReceiver>
    {
        private HttpRequestMessage _postRequest;

		private const string TestContent = "{ \"key\": \"value\" }";
		private const string TestId = "";

		[Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PATCH")]
        [InlineData("PUT")]
        [InlineData("OPTIONS")]
        public async Task ReceiveAsync_ReturnsError_IfInvalidMethod(string method)
        {
            // Arrange
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
            _postRequest.Content = new StringContent(TestContent, Encoding.UTF8, "application/json");
        }
    }
}
