using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace CurrencyConverter.Tests.Services
{
    public class ResiliencePolicyTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public ResiliencePolicyTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public async Task CreateHttpResiliencePolicy_RetriesOnTransientErrors()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object);

            // Setup to fail with a transient error on first call, then succeed
            var sequence = handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                );

            sequence.ThrowsAsync(new HttpRequestException("Connection error"));
            sequence.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Success") });

            var resiliencePolicy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);

            // Act
            var result = await resiliencePolicy.ExecuteAsync(async () =>
            {
                var response = await httpClient.GetAsync("https://example.com");
                response.EnsureSuccessStatusCode();
                return response;
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            // Verify the handler was called twice (first fails, second succeeds)
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task CreateHttpResiliencePolicy_RetriesOnServerErrors()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object);

            // Setup to fail with a 500 error on first call, then succeed
            var sequence = handlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                );

            sequence.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            sequence.ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Success") });

            var resiliencePolicy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);

            // Act
            var result = await resiliencePolicy.ExecuteAsync(async () =>
            {
                var response = await httpClient.GetAsync("https://example.com");
                response.EnsureSuccessStatusCode();
                return response;
            });

            // Assert
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);

            // Verify the handler was called twice (first fails, second succeeds)
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task CreateHttpResiliencePolicy_EventuallyFailsAfterMaxRetries()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(handlerMock.Object);

            // Setup to always fail with a transient error
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Connection error"));

            var resiliencePolicy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await resiliencePolicy.ExecuteAsync(async () =>
                {
                    var response = await httpClient.GetAsync("https://example.com");
                    response.EnsureSuccessStatusCode();
                    return response;
                });
            });

            // Verify the handler was called the expected number of times (1 initial + 3 retries)
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(4),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
