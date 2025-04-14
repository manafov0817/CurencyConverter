using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace CurrencyConverter.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred during request processing");
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            var response = new ErrorResponse
            {
                TraceId = Activity.Current?.Id ?? context.TraceIdentifier
            };

            switch (exception)
            {
                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.Message = "The requested resource was not found.";
                    break;
                
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.StatusCode = HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access.";
                    break;
                
                case ArgumentException:
                case FormatException:
                case ValidationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    break;
                
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    response.Message = _environment.IsDevelopment() 
                        ? exception.Message 
                        : "An unexpected error occurred. Please try again later.";
                    break;
            }

            // Include exception details in development environment
            if (_environment.IsDevelopment())
            {
                response.DeveloperMessage = exception.ToString();
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(json);
        }
    }

    public record ErrorResponse
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? DeveloperMessage { get; set; }
        public string? TraceId { get; set; }
    }

    // Custom exception for validation errors
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}
