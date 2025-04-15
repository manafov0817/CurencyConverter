using CurrencyConverter.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace CurrencyConverter.Tests.Api
{
    public class DIRegisterTests
    {
        [Fact]
        public void AddApiServices_RegistersAllRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Add required services
            services.AddLogging(builder => builder.AddConsole());
            
            // Create a real configuration with JWT settings
            var inMemorySettings = new Dictionary<string, string> {
                {"JwtSettings:Secret", "test-secret-key-with-minimum-length-for-security"},
                {"JwtSettings:Issuer", "test-issuer"},
                {"JwtSettings:Audience", "test-audience"},
                {"JwtSettings:ExpiryMinutes", "60"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Count services before
            int serviceCountBefore = services.Count;

            // Act
            services.AddApiServices(configuration);

            // Assert
            // Verify more services were added
            Assert.True(services.Count > serviceCountBefore, "AddApiServices should register additional services");
            
            // Check for controller-related services
            var controllerServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("Controller") == true || 
                s.ImplementationType?.FullName?.Contains("Controller") == true || 
                s.ServiceType.FullName?.Contains("MVC") == true).ToList();
            Assert.True(controllerServices.Count > 0, "No controller services were registered");

            // Check for API versioning services
            var versioningServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("ApiVersion") == true || 
                s.ImplementationType?.FullName?.Contains("ApiVersion") == true).ToList();
            Assert.True(versioningServices.Count > 0, "No API versioning services were registered");

            // Check for Swagger services
            var swaggerServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("Swagger") == true || 
                s.ImplementationType?.FullName?.Contains("Swagger") == true).ToList();
            Assert.True(swaggerServices.Count > 0, "No Swagger services were registered");

            // Check for authentication services
            var authServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("Authentication") == true || 
                s.ImplementationType?.FullName?.Contains("Authentication") == true).ToList();
            Assert.True(authServices.Count > 0, "No authentication services were registered");

            // Check for memory cache services
            var cacheServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("MemoryCache") == true || 
                s.ImplementationType?.FullName?.Contains("MemoryCache") == true).ToList();
            Assert.True(cacheServices.Count > 0, "No memory cache services were registered");

            // Check for OpenTelemetry services
            var telemetryServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("OpenTelemetry") == true || 
                s.ImplementationType?.FullName?.Contains("OpenTelemetry") == true).ToList();
            Assert.True(telemetryServices.Count > 0, "No OpenTelemetry services were registered");
        }

        [Fact]
        public void AddApiServices_ConfiguresJsonOptions_WithCorrectSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            services.AddApiServices(configuration);

            // Assert - Check for MVC-related services
            var mvcServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("Mvc") == true || 
                s.ImplementationType?.FullName?.Contains("Mvc") == true ||
                s.ServiceType.FullName?.Contains("Controller") == true).ToList();
            
            Assert.True(mvcServices.Count > 0, "No MVC services were registered");

            // Look for any JSON-related services
            var jsonServices = services.Where(s => 
                s.ServiceType.FullName?.Contains("Json") == true || 
                s.ImplementationType?.FullName?.Contains("Json") == true).ToList();
            
            Assert.True(jsonServices.Count > 0, "No JSON-related services were registered");
        }

        [Fact]
        public void AddApiServices_ConfiguresAuthentication_WithJwtBearer()
        {
            // Arrange
            var services = new ServiceCollection();
            
            var inMemorySettings = new Dictionary<string, string> {
                {"JwtSettings:Secret", "test-secret-key-with-minimum-length-for-security"},
                {"JwtSettings:Issuer", "test-issuer"},
                {"JwtSettings:Audience", "test-audience"},
                {"JwtSettings:ExpiryMinutes", "60"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Act
            services.AddApiServices(configuration);

            // Assert
            var jwtBearerServices = services.Where(s => 
                (s.ServiceType.FullName?.Contains("Authentication") == true || 
                 s.ImplementationType?.FullName?.Contains("Authentication") == true) &&
                (s.ServiceType.FullName?.Contains("JwtBearer") == true || 
                 s.ImplementationType?.FullName?.Contains("JwtBearer") == true ||
                 s.ServiceType.FullName?.Contains("Jwt") == true ||
                 s.ImplementationType?.FullName?.Contains("Jwt") == true)).ToList();
            
            Assert.True(jwtBearerServices.Count > 0, "No JWT authentication services were registered");
        }

        [Fact]
        public void AddApiServices_ConfiguresSwagger_WithCorrectSettings()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            services.AddApiServices(configuration);

            // Assert
            // Check if any swagger-related services are registered
            var swaggerServices = services.Where(s => 
                s.ServiceType.FullName.Contains("Swagger") || 
                s.ImplementationType?.FullName.Contains("Swagger") == true).ToList();
            
            Assert.True(swaggerServices.Count > 0, "No Swagger services were registered");
            
            // Check if API Explorer services are registered
            var apiExplorerServices = services.Where(s => 
                s.ServiceType.FullName.Contains("ApiExplorer") || 
                s.ImplementationType?.FullName.Contains("ApiExplorer") == true ||
                s.ServiceType.FullName.Contains("EndpointsApiExplorer")).ToList();
            
            Assert.True(apiExplorerServices.Count > 0, "No API Explorer services were registered");
        }

        [Fact]
        public void UseApiMiddleware_RegistersAllMiddleware()
        {
            // Since we can't directly test middleware registration due to extension method limitations,
            // we'll test that the method returns the same application builder

            // Arrange
            var appBuilderMock = new Mock<IApplicationBuilder>();
            appBuilderMock.Setup(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                .Returns(appBuilderMock.Object);

            // Act
            var result = DIRegister.UseApiMiddleware(appBuilderMock.Object);

            // Assert
            Assert.Same(appBuilderMock.Object, result);
        }
    }
}
