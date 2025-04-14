using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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

            // Enable buffering to read the response body
            context.Response.Body = new MemoryStream();

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);

                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation(
                    "Request: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method} | Response: {StatusCode} | Time: {ElapsedMs}ms",
                    path, 
                    queryString, 
                    clientIp, 
                    clientId, 
                    method, 
                    statusCode, 
                    elapsedMs);

                // Copy the response body to the original stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                var originalBodyStream = context.Response.Body;
                await context.Response.Body.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Request failed: {Path}{QueryString} | Client: {ClientIp} | ClientId: {ClientId} | Method: {Method} | Time: {ElapsedMs}ms",
                    path, 
                    queryString, 
                    clientIp, 
                    clientId, 
                    method, 
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
