using CurrencyConverter.Api;
using CurrencyConverter.Core;
using CurrencyConverter.Infrastructure;
using CurrencyConverter.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace CurrencyConverter.Tests
{
    public class DIRegisterTests
    {
        [Fact]
        public void ApiDIRegister_AddApiServices_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add required services
            services.AddLogging(builder => builder.AddConsole());
            
            // Create a real configuration with JWT settings
            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-with-minimum-length-for-security",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiryMinutes = 60
            };
            
            var inMemorySettings = new Dictionary<string, string> {
                {"JwtSettings:Secret", jwtSettings.Secret},
                {"JwtSettings:Issuer", jwtSettings.Issuer},
                {"JwtSettings:Audience", jwtSettings.Audience},
                {"JwtSettings:ExpiryMinutes", jwtSettings.ExpiryMinutes.ToString()}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Count services before
            int serviceCountBefore = services.Count;

            // Act
            services.AddApiServices(configuration);

            // Assert - Just verify the service collection contains more services after adding API services
            Assert.True(services.Count > serviceCountBefore, "AddApiServices should register additional services");
            
            // Check for services related to controllers
            Assert.Contains(services, s => s.ServiceType.Name.Contains("Controller") || 
                                           s.ServiceType.FullName.Contains("Controller") ||
                                           s.ServiceType.FullName.Contains("MVC") ||
                                           s.ServiceType.FullName.Contains("Swagger"));
        }

        //[Fact]
        //public void ApiDIRegister_UseApiMiddleware_RegistersMiddleware()
        //{
        //    // Arrange
        //    var appBuilderMock = new Mock<IApplicationBuilder>();
        //    appBuilderMock.Setup(a => a.UseMiddleware<Api.Middleware.ExceptionHandlingMiddleware>())
        //        .Returns(appBuilderMock.Object);
        //    appBuilderMock.Setup(a => a.UseMiddleware<Api.Middleware.RequestLoggingMiddleware>())
        //        .Returns(appBuilderMock.Object);
        //    appBuilderMock.Setup(a => a.UseMiddleware<Api.Middleware.RateLimitingMiddleware>())
        //        .Returns(appBuilderMock.Object);

        //    // Act
        //    var result = appBuilderMock.Object.UseApiMiddleware();

        //    // Assert
        //    Assert.Same(appBuilderMock.Object, result);
        //    appBuilderMock.Verify(a => a.UseMiddleware<Api.Middleware.ExceptionHandlingMiddleware>(), Times.Once);
        //    appBuilderMock.Verify(a => a.UseMiddleware<Api.Middleware.RequestLoggingMiddleware>(), Times.Once);
        //    appBuilderMock.Verify(a => a.UseMiddleware<Api.Middleware.RateLimitingMiddleware>(), Times.Once);
        //}

        [Fact]
        public void CoreDIRegister_AddCoreServices_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add required services
            services.AddLogging(builder => builder.AddConsole());

            // Count services before
            int serviceCountBefore = services.Count;

            // Act
            services.AddCoreServices();

            // Assert - Just verify the service collection contains more services after adding Core services
            Assert.True(services.Count > serviceCountBefore, "AddCoreServices should register additional services");
            
            // Check for specific service registrations related to Mapster
            Assert.Contains(services, s => s.ServiceType.Name.Contains("Mapper") || 
                                          s.ServiceType.FullName.Contains("Mapster"));
        }

        [Fact]
        public void InfrastructureDIRegister_AddInfrastructureServices_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add required services
            services.AddLogging(builder => builder.AddConsole());
            
            // Create a real configuration with JWT settings
            var jwtSettings = new JwtSettings
            {
                Secret = "test-secret-key-with-minimum-length-for-security",
                Issuer = "test-issuer",
                Audience = "test-audience",
                ExpiryMinutes = 60
            };
            
            var inMemorySettings = new Dictionary<string, string> {
                {"JwtSettings:Secret", jwtSettings.Secret},
                {"JwtSettings:Issuer", jwtSettings.Issuer},
                {"JwtSettings:Audience", jwtSettings.Audience},
                {"JwtSettings:ExpiryMinutes", jwtSettings.ExpiryMinutes.ToString()}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Count services before
            int serviceCountBefore = services.Count;

            // Act
            services.AddInfrastructureServices(configuration);

            // Assert - Just verify the service collection contains more services after adding Infrastructure services
            Assert.True(services.Count > serviceCountBefore, "AddInfrastructureServices should register additional services");
            
            // Check for specific service registrations
            Assert.Contains(services, s => s.ServiceType == typeof(IJwtTokenService));
            Assert.Contains(services, s => s.ServiceType == typeof(Core.Interfaces.ICurrencyProviderFactory));
        }
    }
}
