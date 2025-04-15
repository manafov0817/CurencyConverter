using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Infrastructure.Providers;
using CurrencyConverter.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CurrencyConverter.Tests.Infrastructure
{
    /// <summary>
    /// Mock version of DIRegister for testing purposes
    /// </summary>
    public static class MockDIRegister
    {
        /// <summary>
        /// Adds infrastructure services without the HttpClient registration that causes issues in tests
        /// </summary>
        public static IServiceCollection AddMockInfrastructureServices(this IServiceCollection services,
                                                                      IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
                
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<ICurrencyProviderFactory, CurrencyProviderFactory>();
            services.AddTransient<FrankfurterApiProvider>();
            services.AddTransient<ICurrencyProvider, FrankfurterApiProvider>();
            
            // Skip the HttpClient registration that causes issues in tests
            
            return services;
        }
    }
}
