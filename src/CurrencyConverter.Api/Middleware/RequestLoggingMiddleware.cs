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
            // Get the stopwatch from the PerformanceTrackingMiddleware
            var stopwatch = context.Items.ContainsKey("RequestStopwatch") 
                ? (Stopwatch)context.Items["RequestStopwatch"] 
                : null;
                
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

                var statusCode = context.Response.StatusCode;

                _logger.LogInformation(
                    "Request: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method} | Response: {StatusCode}",
                    path,
                    queryString,
                    clientIp,
                    clientId,
                    method,
                    statusCode);

                // Reset the response body stream position
                responseBodyStream.Seek(0, SeekOrigin.Begin);

                // Copy the response body to the original stream and to the response
                await responseBodyStream.CopyToAsync(originalBodyStream);
                
                // Restore the original response body stream
                context.Response.Body = originalBodyStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Request failed: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method}",
                    path,
                    queryString,
                    clientIp,
                    clientId,
                    method);
                
                // Restore the original response body stream
                context.Response.Body = originalBodyStream;
                
                throw;
            }
        }
    }
}
