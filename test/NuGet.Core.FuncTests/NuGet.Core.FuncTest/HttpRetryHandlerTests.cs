﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Test.Server;
using NuGet.Test.Utility;
using Test.Utility;
using Xunit;

namespace NuGet.Core.FuncTest
{
    public class HttpRetryHandlerTests
    {
        private const string TestUrl = "https://test.local/test.json";

        [Fact]
        public async Task HttpRetryHandler_AppliesTimeoutToRequestsIndividually()
        {
            // Arrange

            // 20 requests that take 250ms each for a total of 5 seconds (plus noise).
            var requestDuration = TimeSpan.FromMilliseconds(250);
            var maxTries = 20;

            // Make the request timeout longer than each request duration but less than the total
            // duration of all attempts.
            var requestTimeout = TimeSpan.FromMilliseconds(4000);

            var hits = 0;
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler = async (requestMessage, token) =>
            {
                hits++;
                await Task.Delay(requestDuration);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            };

            var retryHandler = new HttpRetryHandler();
            var testHandler = new HttpRetryTestHandler(handler);
            var httpClient = new HttpClient(testHandler);
            var request = new HttpRetryHandlerRequest(httpClient, () => new HttpRequestMessage(HttpMethod.Get, TestUrl))
            {
                MaxTries = maxTries,
                RequestTimeout = requestTimeout,
                RetryDelay = TimeSpan.Zero
            };
            var log = new TestLogger();

            // Act
            using (await retryHandler.SendAsync(request, log, CancellationToken.None))
            {
            }

            // Assert
            Assert.Equal(maxTries, hits);
        }

        [Fact]
        public async Task HttpRetryHandler_HandlesFailureToConnect()
        {
            // Arrange
            var server = new NotListeningServer { Mode = TestServerMode.ConnectFailure };

            // Act & Assert
            var exception = await ThrowsException<HttpRequestException>(server);
#if IS_CORECLR
            Assert.NotNull(exception.InnerException);
            if (!RuntimeEnvironmentHelper.IsWindows)
            {
                Assert.Equal("Couldn't connect to server", exception.InnerException.Message);
            }
            else
            {
                Assert.Equal("A connection with the server could not be established", exception.InnerException.Message);
            }
#else
            var innerException = Assert.IsType<WebException>(exception.InnerException);
            Assert.Equal(WebExceptionStatus.ConnectFailure, innerException.Status);
#endif
        }

        [Fact]
        public async Task HttpRetryHandler_HandlesInvalidProtocol()
        {
            // Arrange
            var server = new TcpListenerServer { Mode = TestServerMode.ServerProtocolViolation };

            // Act & Assert
            var exception = await ThrowsException<HttpRequestException>(server);
#if IS_CORECLR
            if (!RuntimeEnvironmentHelper.IsWindows)
            {
                Assert.Null(exception.InnerException);
                Assert.Equal("The server returned an invalid or unrecognized response.", exception.Message);
            }
            else
            {
                Assert.NotNull(exception.InnerException);
                Assert.Equal("The server returned an invalid or unrecognized response", exception.InnerException.Message);
            }
#else
            var innerException = Assert.IsType<WebException>(exception.InnerException);
            Assert.Equal(WebExceptionStatus.ServerProtocolViolation, innerException.Status);
#endif
        }

        [Fact]
        public async Task HttpRetryHandler_HandlesNameResolutionFailure()
        {
            // Arrange
            var server = new UnknownDnsServer { Mode = TestServerMode.NameResolutionFailure };

            // Act & Assert
            var exception = await ThrowsException<HttpRequestException>(server);
#if IS_CORECLR
            Assert.NotNull(exception.InnerException);
            if (!RuntimeEnvironmentHelper.IsWindows)
            {
                Assert.Equal("Couldn't resolve host name", exception.InnerException.Message);
            }
            else
            {
                Assert.Equal("The server name or address could not be resolved", exception.InnerException.Message);
            }
#else
            var innerException = Assert.IsType<WebException>(exception.InnerException);
            Assert.Equal(WebExceptionStatus.NameResolutionFailure, innerException.Status);
#endif
        }

        private static async Task<T> ThrowsException<T>(ITestServer server) where T : Exception
        {
            return await server.ExecuteAsync(async address =>
            {
                // Arrange
                var retryHandler = new HttpRetryHandler();
                var countingHandler = new CountingHandler { InnerHandler = new HttpClientHandler() };
                var httpClient = new HttpClient(countingHandler);
                var request = new HttpRetryHandlerRequest(httpClient, () => new HttpRequestMessage(HttpMethod.Get, address))
                {
                    MaxTries = 2,
                    RetryDelay = TimeSpan.Zero
                };

                // Act
                Func<Task> actionAsync = () => retryHandler.SendAsync(
                    request,
                    new TestLogger(),
                    CancellationToken.None);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<T>(actionAsync);
                Assert.Equal(2, countingHandler.Hits);
                return exception;
            });
        }

        private class CountingHandler : DelegatingHandler
        {
            public int Hits { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Hits++;

                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
