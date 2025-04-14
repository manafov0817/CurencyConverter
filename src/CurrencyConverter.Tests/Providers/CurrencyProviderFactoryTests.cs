using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Infrastructure.Providers;
using Moq;
using Xunit;

namespace CurrencyConverter.Tests.Providers
{
    public class CurrencyProviderFactoryTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ICurrencyProvider> _mockFrankfurterProvider;
        private readonly CurrencyProviderFactory _factory;

        public CurrencyProviderFactoryTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockFrankfurterProvider = new Mock<ICurrencyProvider>();

            _mockServiceProvider
                .Setup(x => x.GetService(typeof(FrankfurterApiProvider)))
                .Returns(_mockFrankfurterProvider.Object);

            _factory = new CurrencyProviderFactory(_mockServiceProvider.Object);
        }

        [Fact]
        public void GetProvider_WithDefaultProvider_ReturnsFrankfurterProvider()
        {
            // Act
            var provider = _factory.GetProvider();

            // Assert
            Assert.Same(_mockFrankfurterProvider.Object, provider);
            _mockServiceProvider.Verify(x => x.GetService(typeof(FrankfurterApiProvider)), Times.Once);
        }

        [Fact]
        public void GetProvider_WithFrankfurterProvider_ReturnsFrankfurterProvider()
        {
            // Act
            var provider = _factory.GetProvider("Frankfurter");

            // Assert
            Assert.Same(_mockFrankfurterProvider.Object, provider);
            _mockServiceProvider.Verify(x => x.GetService(typeof(FrankfurterApiProvider)), Times.Once);
        }

        [Fact]
        public void GetProvider_WithCaseInsensitiveProvider_ReturnsFrankfurterProvider()
        {
            // Act
            var provider = _factory.GetProvider("frankfurter");

            // Assert
            Assert.Same(_mockFrankfurterProvider.Object, provider);
            _mockServiceProvider.Verify(x => x.GetService(typeof(FrankfurterApiProvider)), Times.Once);
        }

        [Fact]
        public void GetProvider_WithUnsupportedProvider_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _factory.GetProvider("UnsupportedProvider"));
            Assert.Contains("is not supported", exception.Message);
        }

        [Fact]
        public void GetAvailableProviders_ReturnsAllProviders()
        {
            // Act
            var providers = _factory.GetAvailableProviders().ToList();

            // Assert
            Assert.Single(providers);
            Assert.Contains("Frankfurter", providers);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new CurrencyProviderFactory(null));
        }
    }
}
