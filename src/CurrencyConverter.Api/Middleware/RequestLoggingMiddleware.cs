using System.Diagnostics;

namespace CurrencyConverter.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var clientId = context.User.FindFirst("ClientId")?.Value ?? "Unknown";
            var method = context.Request.Method;
            var path = context.Request.Path;
            var queryString = context.Request.QueryString.ToString();

            // Store the original response body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                // Create a new memory stream for the response
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;

                // Call the next middleware in the pipeline
                await _next(context);

                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Add timing header to response (useful for client-side monitoring)
                context.Response.Headers.Add("X-Response-Time-Ms", elapsedMs.ToString());

                _logger.LogInformation(
                    "Request: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method} | Response: {StatusCode} | Duration: {ElapsedMs}ms",
                    path,
                    queryString,
                    clientIp,
                    clientId,
                    method,
                    statusCode,
                    elapsedMs);

                // Reset the response body stream position
                responseBodyStream.Seek(0, SeekOrigin.Begin);

                // Copy the response body to the original stream and to the response
                await responseBodyStream.CopyToAsync(originalBodyStream);
                
                // Restore the original response body stream
                context.Response.Body = originalBodyStream;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                
                _logger.LogError(ex,
                    "Request failed: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method} | Duration: {ElapsedMs}ms",
                    path,
                    queryString,
                    clientIp,
                    clientId,
                    method,
                    elapsedMs);
                
                // Restore the original response body stream
                context.Response.Body = originalBodyStream;
                
                throw;
            }
        }
    }
}
