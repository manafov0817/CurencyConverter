using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Mapping;
using CurrencyConverter.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Core
{
    public static class DIRegister
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // Register core services
            services.AddScoped<ICurrencyService, CurrencyService>();
            
            // Register Mapster
            services.AddMapster();

            return services;
        }
    }
}
