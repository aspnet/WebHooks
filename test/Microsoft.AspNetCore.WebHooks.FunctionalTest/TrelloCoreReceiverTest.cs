// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using TrelloCoreReceiver;
using Xunit;

namespace Microsoft.AspNetCore.WebHooks.FunctionalTest
{
    public class TrelloCoreReceiverTest : IClassFixture<WebHookTestFixture<Startup>>
    {
        private readonly HttpClient _client;
        private readonly WebHookTestFixture<Startup> _fixture;

        public TrelloCoreReceiverTest(WebHookTestFixture<Startup> fixture)
        {
            _client = fixture.CreateClient();
            _fixture = fixture;
        }

        [Fact]
        public async Task HomePage_IsNotFound()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        public static TheoryData<HttpMethod> NonGetOrPostDataSet
        {
            get
            {
                return new TheoryData<HttpMethod>
                {
                    HttpMethod.Delete,
                    HttpMethod.Put,
                };
            }
        }

        [Theory]
        [MemberData(nameof(NonGetOrPostDataSet))]
        public async Task WebHookAction_NonGetOrPost_IsNotAllowed(HttpMethod method)
        {
            // Arrange
            var expectedErrorMessage = $"The 'trello' WebHook receiver does not support the HTTP '{method.Method}' " +
                "method.";
            var request = new HttpRequestMessage(method, "/api/webhooks/incoming/trello");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedErrorMessage, responseText);
        }

        public static TheoryData<HttpMethod> GetOrHeadDataSet
        {
            get
            {
                return new TheoryData<HttpMethod>
                {
                    HttpMethod.Get,
                    HttpMethod.Head,
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetOrHeadDataSet))]
        public async Task WebHookAction_GetOrHead_Succeeds(HttpMethod method)
        {
            // Arrange
            var request = new HttpRequestMessage(method, "/api/webhooks/incoming/trello");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Empty(responseText);
        }

        [Theory]
        [InlineData("Trello1.json", "42rSTYfdESQqUuIO3wOYfRjNvKk=")]
        [InlineData("Trello2.json", "zT4l2OxPV7VbJoIBzYR5AS30f+s=")]
        public async Task WebHookAction_WithBody_Succeeds(string filename, string signatureHeader)
        {
            // Arrange
            var fixture = _fixture.WithTestLogger(out var testSink);
            var client = fixture.CreateClient();

            var path = Path.Combine("Resources", "RequestBodies", filename);
            var stream = await ResourceFile.GetResourceStreamAsync(path, normalizeLineEndings: true);
            var content = new StreamContent(stream)
            {
                Headers =
                {
                    { HeaderNames.ContentLength, stream.Length.ToString() },
                    { HeaderNames.ContentType, "text/json" },
                },
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/incoming/trello")
            {
                Content = content,
                Headers =
                {
                    { TrelloConstants.SignatureHeaderName, signatureHeader },
                },
            };

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Empty(responseText);
        }
    }
}
