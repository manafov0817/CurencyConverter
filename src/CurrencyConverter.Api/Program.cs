using CurrencyConverter.Api;
using CurrencyConverter.Core;
using CurrencyConverter.Infrastructure;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/currency-converter-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
 
// Register services using the DIRegister extension methods
builder.Services
    .AddCoreServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Converter API V1");
    options.RoutePrefix = string.Empty; // Set Swagger UI at the root
});

app.UseHttpsRedirection();

// Use custom middleware from DIRegister
app.UseApiMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
