using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;

namespace CurrencyConverter.Api.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly int _requestLimit;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<RateLimitingMiddleware> logger,
            int requestLimit = 100,
            int timeWindowMinutes = 15)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _requestLimit = requestLimit;
            _timeWindow = TimeSpan.FromMinutes(timeWindowMinutes);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = context.User.FindFirst("ClientId")?.Value;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var endpoint = context.Request.Path;

            // Use client ID if available, otherwise fall back to IP address
            var requestKey = !string.IsNullOrEmpty(clientId)
                ? $"RateLimit_{clientId}_{endpoint}"
                : $"RateLimit_{clientIp}_{endpoint}";

            // Get current count for the client
            if (!_cache.TryGetValue(requestKey, out int requestCount))
            {
                requestCount = 0;
            }

            if (requestCount >= _requestLimit)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientIdentifier} on endpoint {Endpoint}",
                    !string.IsNullOrEmpty(clientId) ? clientId : clientIp, endpoint);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";

                var response = JsonSerializer.Serialize(new
                {
                    Status = 429,
                    Message = "Too many requests. Please try again later."
                });

                await context.Response.WriteAsync(response);
                return;
            }

            // Increment the request count and set cache expiration
            _cache.Set(requestKey, requestCount + 1, _timeWindow);

            await _next(context);
        }
    }
}
