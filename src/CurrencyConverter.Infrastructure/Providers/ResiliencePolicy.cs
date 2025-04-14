using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace CurrencyConverter.Infrastructure.Providers
{
    public static class ResiliencePolicy
    {
        public static IAsyncPolicy<HttpResponseMessage> CreateHttpResiliencePolicy(ILogger logger)
        {
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger.LogWarning(
                            "Request failed with {StatusCode}. Retrying in {RetryTimespan}s. Attempt {RetryAttempt}/3",
                            outcome.Result?.StatusCode, timespan.TotalSeconds, retryAttempt);
                    });

            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (outcome, timespan) =>
                    {
                        logger.LogError(
                            "Circuit breaker opened for {DurationOfBreak}s due to failures",
                            timespan.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        logger.LogInformation("Circuit breaker reset. Normal operation resumed");
                    },
                    onHalfOpen: () =>
                    {
                        logger.LogInformation("Circuit breaker half-open. Testing if service is available");
                    });

            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }
    }
}
