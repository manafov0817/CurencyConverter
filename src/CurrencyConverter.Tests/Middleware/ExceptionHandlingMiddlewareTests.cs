using CurrencyConverter.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Tests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
        private readonly Mock<IHostEnvironment> _mockEnvironment;

        public ExceptionHandlingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
            _mockEnvironment = new Mock<IHostEnvironment>();
        }

        [Fact]
        public async Task InvokeAsync_WithNoException_CallsNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            // Create a new middleware instance with our test delegate
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WithArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new ArgumentException("Invalid argument");
            };

            // Create a new middleware instance with our test delegate
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("Invalid argument", error.Message);
            Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithValidationException_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new ValidationException("Validation failed");
            };

            // Create a new middleware instance with our test delegate
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("Validation failed", error?.Message);
            Assert.Equal(HttpStatusCode.BadRequest, error?.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithUnauthorizedAccessException_ReturnsUnauthorized()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new UnauthorizedAccessException("Unauthorized access");
            };

            // Create a new middleware instance with our test delegate
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("Unauthorized access.", error.Message);
            Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithKeyNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new KeyNotFoundException("Resource not found");
            };

            // Create a new middleware instance with our test delegate
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("The requested resource was not found.", error?.Message);
            Assert.Equal(HttpStatusCode.NotFound, error?.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithGenericException_ReturnsInternalServerError()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new Exception("Something went wrong");
            };

            // Create a new middleware instance with our test delegate
            // Set up the environment to be non-development
            _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object, _mockEnvironment.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var error = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.Equal("An unexpected error occurred. Please try again later.", error.Message);
            Assert.Equal(HttpStatusCode.InternalServerError, error.StatusCode);
        }
    }
}
