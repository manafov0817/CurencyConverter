using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace CurrencyConverter.Infrastructure.Providers
{
    public static class ResiliencePolicy
    {
        public static IAsyncPolicy<HttpResponseMessage> CreateHttpResiliencePolicy(ILogger logger)
        {
            // Define a policy that only retries on specific transient HTTP status codes
            var retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>(ex => 
                {
                    // Only retry on HttpRequestException with transient status codes
                    if (ex.StatusCode.HasValue)
                    {
                        var statusCode = ex.StatusCode.Value;
                        return IsTransientHttpStatusCode(statusCode);
                    }
                    // For exceptions without status code, assume they're transient network issues
                    return true;
                })
                .OrResult(result => IsTransientHttpStatusCode(result.StatusCode))
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        logger.LogWarning(
                            "Request failed with {StatusCode}. Retrying in {RetryTimespan}s. Attempt {RetryAttempt}/3",
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(), 
                            timespan.TotalSeconds, 
                            retryAttempt);
                    });

            // Define a circuit breaker policy with the same error handling logic
            var circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>(ex => 
                {
                    // Only break circuit on HttpRequestException with transient status codes
                    if (ex.StatusCode.HasValue)
                    {
                        var statusCode = ex.StatusCode.Value;
                        return IsTransientHttpStatusCode(statusCode);
                    }
                    // For exceptions without status code, assume they're transient network issues
                    return true;
                })
                .OrResult(result => IsTransientHttpStatusCode(result.StatusCode))
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
        
        // Helper method to determine if an HTTP status code is transient
        private static bool IsTransientHttpStatusCode(HttpStatusCode statusCode)
        {
            // Only retry on server errors (5xx) and specific client errors that might be transient
            return (int)statusCode >= 500 || 
                   statusCode == HttpStatusCode.RequestTimeout || 
                   statusCode == HttpStatusCode.TooManyRequests;
        }
    }
}
