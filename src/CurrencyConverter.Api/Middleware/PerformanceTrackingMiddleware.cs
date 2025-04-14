using System.Diagnostics;

namespace CurrencyConverter.Api.Middleware
{
    public class PerformanceTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceTrackingMiddleware> _logger;

        public PerformanceTrackingMiddleware(
            RequestDelegate next,
            ILogger<PerformanceTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Add the stopwatch to HttpContext items so other middleware can access it
            context.Items["RequestStopwatch"] = stopwatch;

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                var path = context.Request.Path;
                var method = context.Request.Method;
                var statusCode = context.Response.StatusCode;

                // Log performance metrics
                _logger.LogInformation(
                    "Performance: {Method} {Path} | Status: {StatusCode} | Duration: {ElapsedMs}ms",
                    method,
                    path,
                    statusCode,
                    elapsedMs);

                // Add timing header to response (useful for client-side monitoring)
                context.Response.Headers.Add("X-Response-Time-Ms", elapsedMs.ToString());
            }
        }
    }
}
