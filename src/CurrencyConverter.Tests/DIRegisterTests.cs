using CurrencyConverter.Api;
using CurrencyConverter.Core;
using CurrencyConverter.Infrastructure;
using CurrencyConverter.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
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

        [Fact]
        public void ApiDIRegister_UseApiMiddleware_RegistersMiddleware()
        {
            // Since we can't directly mock extension methods, we'll create a simple test
            // that verifies the method returns the same IApplicationBuilder instance

            // Arrange
            var appBuilderMock = new Mock<IApplicationBuilder>();

            // Setup the mock to return itself for any method call
            appBuilderMock.Setup(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
                .Returns(appBuilderMock.Object);

            // Act
            var result = CurrencyConverter.Api.DIRegister.UseApiMiddleware(appBuilderMock.Object);

            // Assert
            Assert.Same(appBuilderMock.Object, result);
            // We can't verify the exact middleware types being used, but we can verify
            // that the method doesn't throw an exception and returns the builder
        }

        [Fact]
        public void ApiDIRegister_ConfigureAuthentication_RegistersJwtAuthentication()
        {
            // Arrange
            var services = new ServiceCollection();

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

            // Act
            services.AddApiServices(configuration);

            // Assert
            var authService = services.FirstOrDefault(s => s.ServiceType.Name.Contains("Authentication"));
            Assert.NotNull(authService);
        }

        [Fact]
        public void ApiDIRegister_ConfigureSwagger_RegistersSwaggerServices()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            services.AddApiServices(configuration);

            // Assert
            Assert.Contains(services, s => s.ServiceType.FullName.Contains("Swagger"));
            Assert.Contains(services, s => s.ServiceType.Name == "IApiVersionDescriptionProvider");
        }

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

        [Fact]
        public void InfrastructureDIRegister_CreateResiliencePolicy_ReturnsPolicy()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<CurrencyConverter.Infrastructure.Providers.FrankfurterApiProvider>>();

            // Use reflection to access the private method
            var methodInfo = typeof(CurrencyConverter.Infrastructure.DIRegister).GetMethod("CreateResiliencePolicy",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act
            var policy = methodInfo.Invoke(null, new object[] { loggerMock.Object });

            // Assert
            Assert.NotNull(policy);
        }

        [Fact]
        public void InfrastructureDIRegister_AddInfrastructureServices_RegistersHttpClient()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    {"JwtSettings:Secret", "test-secret-key-with-minimum-length-for-security"},
                    {"JwtSettings:Issuer", "test-issuer"},
                    {"JwtSettings:Audience", "test-audience"},
                    {"JwtSettings:ExpiryMinutes", "60"}
                })
                .Build();

            // Act
            services.AddInfrastructureServices(configuration);

            // Assert
            Assert.Contains(services, s => s.ServiceType.Name.Contains("HttpClient"));
            Assert.Contains(services, s => s.ServiceType == typeof(CurrencyConverter.Infrastructure.Providers.FrankfurterApiProvider));
            Assert.Contains(services, s => s.ServiceType == typeof(Core.Interfaces.ICurrencyProvider));
        }
    }
}
