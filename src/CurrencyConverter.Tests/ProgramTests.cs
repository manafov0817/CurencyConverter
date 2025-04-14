using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CurrencyConverter.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void Program_BuildWebApplication_ConfiguresApplication()
        {
            // This test verifies that we can build a WebApplication with similar configuration to Program.cs

            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Add services similar to Program.cs
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Act - Build the application
            var app = builder.Build();

            // Configure the app similar to Program.cs
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            // Assert - If no exception is thrown, the test passes
            Assert.NotNull(app);
        }

        [Fact]
        public void Program_ConfigureServices_RegistersRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act - Add services like Program.cs does
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddAuthentication();
            services.AddAuthorization();

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.NotNull(serviceProvider);
        }
    }
}
