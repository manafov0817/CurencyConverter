using CurrencyConverter.Core.Mapping;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CurrencyConverter.Tests.Mapping
{
    public class MapsterConfigTests
    {
        [Fact]
        public void AddMapster_RegistersMapsterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // Act - explicitly call our extension method
            CurrencyConverter.Core.Mapping.MapsterConfig.AddMapster(services);
            
            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Verify TypeAdapterConfig is registered
            var config = serviceProvider.GetService<TypeAdapterConfig>();
            Assert.NotNull(config);
            
            // Verify IMapper is registered
            var mapper = serviceProvider.GetService<IMapper>();
            Assert.NotNull(mapper);
            Assert.IsType<ServiceMapper>(mapper);
        }
    }
}
