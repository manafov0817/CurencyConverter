using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.CircuitBreaker;
using System.Net;
using Xunit;

namespace CurrencyConverter.Tests.Providers
{
    public class ResiliencePolicyTests
    {
        private readonly Mock<ILogger> _mockLogger;

        public ResiliencePolicyTests()
        {
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void CreateHttpResiliencePolicy_ReturnsNonNullPolicy()
        {
            // Act
            var policy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);

            // Assert
            Assert.NotNull(policy);
        }

        [Fact]
        public async Task RetryPolicy_RetriesToExecuteOnTransientError()
        {
            // Arrange
            var policy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);
            var attemptCount = 0;

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    throw new HttpRequestException("Test exception", null, HttpStatusCode.ServiceUnavailable);
                });
            });

            // Should have made 1 original attempt + 3 retries
            Assert.Equal(4, attemptCount);
        }

        [Fact]
        public async Task CircuitBreaker_BreaksCircuitAfterMultipleFailures()
        {
            // Arrange
            var policy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);
            var attemptCount = 0;

            // Act - First round of failures to open the circuit
            for (int i = 0; i < 6; i++)
            {
                try
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        attemptCount++;
                        throw new HttpRequestException("Test exception", null, HttpStatusCode.ServiceUnavailable);
                    });
                }
                catch (HttpRequestException)
                {
                    // Expected
                }
                catch (BrokenCircuitException)
                {
                    // Expected after circuit breaks
                }
            }

            // Act - Try again after circuit is open
            var circuitOpen = false;
            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });
            }
            catch (BrokenCircuitException)
            {
                circuitOpen = true;
            }

            // Assert
            Assert.True(circuitOpen);
            
            // The circuit should have broken after 5 failures (each with 4 attempts due to retry policy)
            // So we expect approximately 20 attempts before the circuit breaks
            Assert.True(attemptCount >= 5);
        }

        [Fact]
        public async Task Policy_SucceedsOnSuccessfulRequest()
        {
            // Arrange
            var policy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            // Act
            var result = await policy.ExecuteAsync(() => Task.FromResult(response));

            // Assert
            Assert.Same(response, result);
        }

        [Fact]
        public async Task Policy_DoesNotRetryOnNonTransientError()
        {
            // Arrange
            var policy = ResiliencePolicy.CreateHttpResiliencePolicy(_mockLogger.Object);
            var attemptCount = 0;

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () =>
            {
                await policy.ExecuteAsync(async () =>
                {
                    attemptCount++;
                    throw new HttpRequestException("Test exception", null, HttpStatusCode.BadRequest);
                });
            });

            // Should have made only 1 attempt since 400 is not a transient error
            Assert.Equal(1, attemptCount);
        }
    }
}
