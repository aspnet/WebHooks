// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GitHubCoreReceiver;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.WebHooks.FunctionalTest
{
    public class GitHubCoreReceiverTest : IClassFixture<WebHookTestFixture<Startup>>
    {
        private readonly HttpClient _client;
        private readonly WebHookTestFixture<Startup> _fixture;

        public GitHubCoreReceiverTest(WebHookTestFixture<Startup> fixture)
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

        [Fact]
        public async Task WebHookAction_NoEventHeader_IsNotFound()
        {
            // Arrange
            var content = new StringContent(string.Empty);
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/incoming/github")
            {
                Content = content,
            };

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var responseText = await response.Content.ReadAsStringAsync();

            // This requirement is enforced in a constraint. Therefore, response is empty.
            Assert.Empty(responseText);
        }

        public static TheoryData<HttpMethod> NonPostDataSet
        {
            get
            {
                return new TheoryData<HttpMethod>
                {
                    HttpMethod.Get,
                    HttpMethod.Head,
                    HttpMethod.Put,
                };
            }
        }

        [Theory]
        [MemberData(nameof(NonPostDataSet))]
        public async Task WebHookAction_NonPost_IsNotAllowed(HttpMethod method)
        {
            // Arrange
            var expectedErrorMessage = $"The 'github' WebHook receiver does not support the HTTP '{method.Method}' " +
                "method.";
            var request = new HttpRequestMessage(method, "/api/webhooks/incoming/github")
            {
                Headers =
                {
                    { GitHubConstants.EventHeaderName, "push" },
                },
            };

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedErrorMessage, responseText);
        }

        [Theory]
        [InlineData("GitHub.Issue_Comment.json", "issue_comment", "sha1=c2e74fd2d35ca5fdcd6e8eb98f53709bfb6fba72")]
        [InlineData("GitHub.Ping.json", "ping", "sha1=116d3b56dccc36ad26cb7cc5349580acfcd0a7ca")]
        [InlineData("GitHub.Push.json", "push", "sha1=3ccec9d0924af86b0b8cc1904c8d5ff4406d55b4")]
        public async Task WebHookAction_WithBody_Succeeds(string filename, string eventName, string signatureHeader)
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
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/incoming/github")
            {
                Content = content,
                Headers =
                {
                    { GitHubConstants.EventHeaderName, eventName },
                    { GitHubConstants.SignatureHeaderName, signatureHeader },
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
