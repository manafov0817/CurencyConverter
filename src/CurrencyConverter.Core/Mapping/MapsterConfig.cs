using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CurrencyConverter.Core.Mapping
{
    public static class MapsterConfig
    {
        public static IServiceCollection AddMapster(this IServiceCollection services)
        {
            // Create and configure TypeAdapterConfig
            var config = TypeAdapterConfig.GlobalSettings;
            
            // Scan for all mapping configurations in the assembly
            config.Scan(Assembly.GetExecutingAssembly());
            
            // Register Mapster as a singleton
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();
            
            return services;
        }
    }
}
