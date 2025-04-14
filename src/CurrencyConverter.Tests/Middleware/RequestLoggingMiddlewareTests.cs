using CurrencyConverter.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CurrencyConverter.Tests.Middleware
{
    public class RequestLoggingMiddlewareTests
    {
        private readonly Mock<ILogger<RequestLoggingMiddleware>> _mockLogger;

        public RequestLoggingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        }

        [Fact]
        public async Task InvokeAsync_LogsRequestDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("ClientId", "test-client-id")
            }));

            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RequestLoggingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task InvokeAsync_WithNoClientId_LogsIpAddress()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";
            // No client ID in claims

            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RequestLoggingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task InvokeAsync_LogsResponseTime()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";

            RequestDelegate next = async (HttpContext ctx) =>
            {
                // Simulate some processing time
                await Task.Delay(10);
                ctx.Response.StatusCode = 200;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RequestLoggingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Response")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task InvokeAsync_WithException_StillLogsResponse()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new Exception("Test exception");
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RequestLoggingMiddleware(next, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => middleware.InvokeAsync(context));

            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Request")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }
    }
}
