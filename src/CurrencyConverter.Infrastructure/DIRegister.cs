using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Infrastructure.Providers;
using CurrencyConverter.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace CurrencyConverter.Infrastructure
{
    public static class DIRegister
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
                                                                   IConfiguration configuration)
        {
            // Register JWT settings
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            // Register provider factory
            services.AddSingleton<ICurrencyProviderFactory, CurrencyProviderFactory>();

            // Configure HttpClient with resilience policies
            services.AddHttpClient<ICurrencyProvider, FrankfurterApiProvider>()
                .AddPolicyHandler((serviceProvider, request) =>
                    CreateResiliencePolicy(serviceProvider.GetRequiredService<ILogger<FrankfurterApiProvider>>()));

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> CreateResiliencePolicy(ILogger logger)
        {
            // Retry policy with exponential backoff
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

            // Circuit breaker policy
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

            // Combine policies
            return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
        }
    }
}
