﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.WebHooks.Filters
{
    public class TrelloVerifySignatureFilterTests
    {
        private const string TestContent = "{ \"type\": \"action\", \"name\": \"Jason\" }";
        private const string TestId = "";
        private const string TestSecret = "12345678901234567890123456789012";
        private const string TestUrl = "https://api.contoso.com/api/webhooks/incoming/trello";

        private static readonly string TestSignatureHeader = GetSignatureHeader(TestContent);

        public static TheoryData<string> InvalidReceiverData
        {
            get
            {
                return new TheoryData<string>
                {
                    { string.Empty },
                    { "你好" },
                    { "1" },
                    { "1234567890" },
                    { "github" },
                };
            }
        }

        public static TheoryData<string> ValidIdData
        {
            get
            {
                return new TheoryData<string>
                {
                    { string.Empty },
                    { "id" },
                    { "你好" },
                    { "1" },
                    { "1234567890" },
                };
            }
        }

        [Fact]
        public void ReceiverName_IsConsistent()
        {
            // Arrange
            var filter = GetFilter();
            var expected = "trello";

            // Act
            var actual = filter.ReceiverName;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(InvalidReceiverData))]
        public void IsApplicable_ReturnsFalseForNonMatches(string receiverName)
        {
            // Arrange
            var filter = GetFilter();

            // Act
            var actual = filter.IsApplicable(receiverName);

            // Assert
            Assert.False(actual);
        }

        [Theory]
        [InlineData("trello")]
        [InlineData("Trello")]
        [InlineData("TRELLO")]
        public void IsApplicable_ReturnsTrueForMatches(string receiverName)
        {
            // Arrange
            var filter = GetFilter();

            // Act
            var actual = filter.IsApplicable(receiverName);

            // Assert
            Assert.True(actual);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_Fails_IfPostHasNoSignatureHeader()
        {
            // Arrange
            var expectedMessage = $"Expecting exactly one '{TrelloConstants.SignatureHeaderName}' header field in " +
                "the WebHook request but found 0. Please ensure the request contains exactly one " +
                $"'{TrelloConstants.SignatureHeaderName}' header field.";
            var filter = GetFilter(TestSecret);
            var context = GetContext(TestContent);

            // Act
            await filter.OnResourceExecutionAsync(context, () => throw new InvalidOperationException("Unreachable"));

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal(expectedMessage, result.Value);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_Fails_IfPostHasTwoSignatureHeaders()
        {
            // Arrange
            var expectedMessage = $"Expecting exactly one '{TrelloConstants.SignatureHeaderName}' header field in " +
                "the WebHook request but found 2. Please ensure the request contains exactly one " +
                $"'{TrelloConstants.SignatureHeaderName}' header field.";
            var filter = GetFilter(TestSecret);
            var context = GetContext(TestContent);
            context.HttpContext.Request.Headers.Add(TrelloConstants.SignatureHeaderName, "value1");
            context.HttpContext.Request.Headers.Append(TrelloConstants.SignatureHeaderName, "value2");

            // Act
            await filter.OnResourceExecutionAsync(context, () => throw new InvalidOperationException("Unreachable"));

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal(expectedMessage, result.Value);
        }

        [Theory]
        [InlineData("你好世界")]
        [InlineData("in++valid")]
        [InlineData("==")]
        [InlineData(",=,=,")]
        public async Task OnResourceExecutionAsync_Fails_IfPostHasInvalidSignatureHeader(string header)
        {
            // Arrange
            var expectedMessage = $"The '{TrelloConstants.SignatureHeaderName}' header value is invalid. The " +
                "'trello' receiver requires a valid Base64-encoded string.";
            var filter = GetFilter(TestSecret);
            var context = GetContext(TestContent);
            context.HttpContext.Request.Headers.Add(TrelloConstants.SignatureHeaderName, header);

            // Act
            await filter.OnResourceExecutionAsync(context, () => throw new InvalidOperationException("Unreachable"));

            // Assert
            var result = Assert.IsType<BadRequestObjectResult>(context.Result);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal(expectedMessage, result.Value);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_Succeeds_IfPostIsNotJson()
        {
            // Arrange
            var filter = GetFilter(TestSecret);
            var context = GetContext(TestContent);
            context.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
            context.HttpContext.Request.Headers.Add(TrelloConstants.SignatureHeaderName, TestSignatureHeader);
            var nextCount = 0;

            // Act
            await filter.OnResourceExecutionAsync(context, () =>
            {
                nextCount++;
                var executedContext = new ResourceExecutedContext(context, context.Filters)
                {
                    Result = context.Result,
                };

                return Task.FromResult(executedContext);
            });

            // Assert
            Assert.Null(context.Result);
            Assert.Equal(1, nextCount);
        }

        [Theory]
        [MemberData(nameof(ValidIdData))]
        public async Task OnResourceExecutionAsync_Succeeds_IfValidPostRequest(string id)
        {
            // Arrange
            var configurationId = string.IsNullOrEmpty(id) ? "default" : id;
            var settings = new Dictionary<string, string>
            {
                { $"WebHooks:Trello:SecretKey:{configurationId}", TestSecret },
            };

            var filter = GetFilter(settings);
            var context = GetContext(TestContent);
            context.HttpContext.Request.Headers.Add(TrelloConstants.SignatureHeaderName, TestSignatureHeader);
            context.RouteData.Values.Add(WebHookConstants.IdKeyName, id);
            var nextCount = 0;

            // Act
            await filter.OnResourceExecutionAsync(context, () =>
            {
                nextCount++;
                var executedContext = new ResourceExecutedContext(context, context.Filters)
                {
                    Result = context.Result,
                };

                return Task.FromResult(executedContext);
            });

            // Assert
            Assert.Null(context.Result);
            Assert.Equal(1, nextCount);
        }

        [Fact]
        public async Task OnResourceExecutionAsync_Succeeds_IfNoReceiverName()
        {
            // Arrange
            var filter = GetFilter(TestSecret);
            var context = GetContext(TestContent);
            context.HttpContext.Request.Headers.Add(TrelloConstants.SignatureHeaderName, TestSignatureHeader);
            context.RouteData.Values.Remove(WebHookConstants.ReceiverKeyName);
            var nextCount = 0;

            // Act
            await filter.OnResourceExecutionAsync(context, () =>
            {
                nextCount++;
                var executedContext = new ResourceExecutedContext(context, context.Filters)
                {
                    Result = context.Result,
                };

                return Task.FromResult(executedContext);
            });

            // Assert
            Assert.Null(context.Result);
            Assert.Equal(1, nextCount);
        }

        [Theory]
        [MemberData(nameof(InvalidReceiverData))]
        public async Task OnResourceExecutionAsync_Succeeds_IfNotApplicable(string receiverName)
        {
            // Arrange
            var filter = GetFilter(TestSecret);
            var context = GetContext(TestContent);
            context.HttpContext.Request.Headers.Add(TrelloConstants.SignatureHeaderName, TestSignatureHeader);
            context.RouteData.Values[WebHookConstants.ReceiverKeyName] = receiverName;
            var nextCount = 0;

            // Act
            await filter.OnResourceExecutionAsync(context, () =>
            {
                nextCount++;
                var executedContext = new ResourceExecutedContext(context, context.Filters)
                {
                    Result = context.Result,
                };

                return Task.FromResult(executedContext);
            });

            // Assert
            Assert.Null(context.Result);
            Assert.Equal(1, nextCount);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("HEAD")]
        [InlineData("PATCH")]
        [InlineData("PUT")]
        [InlineData("OPTIONS")]
        public async Task OnResourceExecutionAsync_Succeeds_IfInvalidMethod(string method)
        {
            // Arrange
            var filter = GetFilter(TestSecret);
            var context = GetContext(TestContent);
            context.HttpContext.Request.Headers.Add(TrelloConstants.SignatureHeaderName, TestSignatureHeader);
            context.HttpContext.Request.Method = method;
            var nextCount = 0;

            // Act
            await filter.OnResourceExecutionAsync(context, () =>
            {
                nextCount++;
                var executedContext = new ResourceExecutedContext(context, context.Filters)
                {
                    Result = context.Result,
                };

                return Task.FromResult(executedContext);
            });

            // Assert
            Assert.Null(context.Result);
            Assert.Equal(1, nextCount);
        }

        private static ResourceExecutingContext GetContext(string content)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(content));
            request.ContentType = "application/json";
            request.IsHttps = true;
            request.Method = HttpMethods.Post;

            // https://api.contoso.com/api/webhooks/incoming/trello
            request.Host = new HostString("api.contoso.com");
            request.Path = "/api/webhooks/incoming/trello";
            request.Scheme = Uri.UriSchemeHttps;

            var routeData = new RouteData();
            routeData.Values.Add(WebHookConstants.ReceiverKeyName, TrelloConstants.ReceiverName);

            return new ResourceExecutingContext(
                new ActionContext(httpContext, routeData, new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new List<IValueProviderFactory>());
        }

        private static TrelloVerifySignatureFilter GetFilter(IDictionary<string, string> settings = null)
        {
            var builder = new ConfigurationBuilder();
            if (settings != null)
            {
                builder.AddInMemoryCollection(settings);
            }

            return new TrelloVerifySignatureFilter(
                builder.Build(),
                Mock.Of<IHostingEnvironment>(),
                NullLoggerFactory.Instance);
        }

        private static TrelloVerifySignatureFilter GetFilter(string secretKey)
        {
            var settings = new Dictionary<string, string>
            {
                { "WebHooks:Trello:SecretKey:default", secretKey },
            };

            return GetFilter(settings);
        }

        private static string GetSignatureHeader(string content)
        {
            var secret = Encoding.UTF8.GetBytes(TestSecret);
            var fullContent = $"{content}{TestUrl}";
            var data = Encoding.UTF8.GetBytes(fullContent);

            using (var hasher = new HMACSHA1(secret))
            {
                // Trello doesn't URI-escape the signature i.e. '/', '+' and '=' characters are unchanged in header.
                var testHash = hasher.ComputeHash(data);
                var signature = Convert.ToBase64String(testHash, Base64FormattingOptions.None);

                return signature;
            }
        }
    }
}
