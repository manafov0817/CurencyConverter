using CurrencyConverter.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace CurrencyConverter.Tests.Middleware
{
    public class RateLimitingMiddlewareTests
    {
        private readonly Mock<ILogger<RateLimitingMiddleware>> _mockLogger;
        private readonly IMemoryCache _memoryCache;
        private readonly RateLimitingMiddleware _middleware;
        private readonly RequestDelegate _next;
        private const int _limit = 100;
        private const int _periodInMinutes = 1;

        public RateLimitingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<RateLimitingMiddleware>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _next = (HttpContext context) => Task.CompletedTask;
            _middleware = new RateLimitingMiddleware(_next, _memoryCache, _mockLogger.Object, _limit, _periodInMinutes);
        }

        [Fact]
        public async Task InvokeAsync_FirstRequest_AllowsRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("ClientId", "test-client-id")
            }));
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";
            var nextCalled = false;

            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, _limit, _periodInMinutes);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_ExceedsRateLimit_ReturnsTooManyRequests()
        {
            // Arrange
            var clientId = "test-client-id";
            var path = "/api/v1/Currency/rates";
            var cacheKey = $"RateLimit_{clientId}_{path}";

            // Set up the cache to simulate rate limit exceeded
            _memoryCache.Set(cacheKey, _limit, TimeSpan.FromMinutes(1));

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("ClientId", clientId)
            }));
            context.Request.Method = "GET";
            context.Request.Path = path;
            context.Response.Body = new MemoryStream();

            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, _limit, _periodInMinutes);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.False(nextCalled);
            Assert.Equal((int)HttpStatusCode.TooManyRequests, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithNoClientId_UsesIpAddress()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
            context.Request.Method = "GET";
            context.Request.Path = "/api/v1/Currency/rates";
            var nextCalled = false;

            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our test delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, _limit, _periodInMinutes);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_MultipleRequestsWithinLimit_AllowsRequests()
        {
            // Arrange
            var clientId = "test-client-id";
            var path = "/api/v1/Currency/rates";

            var context = new DefaultHttpContext();
            context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("ClientId", clientId)
            }));
            context.Request.Method = "GET";
            context.Request.Path = path;

            var nextCalled = 0;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled++;
                return Task.CompletedTask;
            };
            
            // Create a new middleware instance with our counting delegate
            var middleware = new RateLimitingMiddleware(next, _memoryCache, _mockLogger.Object, _limit, _periodInMinutes);

            // Act - Make 5 requests (below the limit of 100)
            for (int i = 0; i < 5; i++)
            {
                await middleware.InvokeAsync(context);
            }

            // Assert
            Assert.Equal(5, nextCalled);
            Assert.Equal((int)HttpStatusCode.OK, context.Response.StatusCode);
        }
    }
}
