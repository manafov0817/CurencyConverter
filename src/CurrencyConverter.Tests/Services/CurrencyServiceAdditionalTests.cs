using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models.Currency;
using CurrencyConverter.Core.Services;
using CurrencyConverter.Core.Utilities;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CurrencyConverter.Tests.Services
{
    public class CurrencyServiceAdditionalTests
    {
        private readonly Mock<ICurrencyProviderFactory> _mockProviderFactory;
        private readonly Mock<ICurrencyProvider> _mockProvider;
        private readonly Mock<ILogger<CurrencyService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly IMemoryCache _cache;
        private readonly CurrencyService _service;

        public CurrencyServiceAdditionalTests()
        {
            _mockProviderFactory = new Mock<ICurrencyProviderFactory>();
            _mockProvider = new Mock<ICurrencyProvider>();
            _mockLogger = new Mock<ILogger<CurrencyService>>();
            _mockMapper = new Mock<IMapper>();

            _cache = new MemoryCache(new MemoryCacheOptions());

            _mockProviderFactory.Setup(f => f.GetProvider(It.IsAny<string>()))
                .Returns(_mockProvider.Object);

            _service = new CurrencyService(_mockProviderFactory.Object, _cache, _mockLogger.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithNullBaseCurrency_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetLatestRatesAsync(null));
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithEmptyBaseCurrency_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetLatestRatesAsync(string.Empty));
        }

        [Fact]
        public async Task GetLatestRatesAsync_WhenProviderThrowsException_PropagatesException()
        {
            // Arrange
            var baseCurrency = "USD";
            _mockProvider.Setup(p => p.GetLatestRatesAsync(baseCurrency))
                .ThrowsAsync(new HttpRequestException("API unavailable"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
                _service.GetLatestRatesAsync(baseCurrency));

            Assert.Equal("API unavailable", exception.Message);
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithCachedData_ReturnsCachedData()
        {
            // Arrange
            var baseCurrency = "USD";
            var cachedResponse = new ExchangeRateResponse
            {
                Amount = 1,
                BaseCurrency = baseCurrency,
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m },
                    { "GBP", 0.75m }
                }
            };

            var cacheKey = CacheKeys.LatestRates(baseCurrency);
            _cache.Set(cacheKey, cachedResponse, TimeSpan.FromHours(1));

            // Act
            var result = await _service.GetLatestRatesAsync(baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedResponse, result);

            // Verify provider was not called
            _mockProvider.Verify(p => p.GetLatestRatesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.ConvertCurrencyAsync(null));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithNullFromCurrency_ThrowsArgumentException()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 100,
                FromCurrency = null,
                ToCurrency = "EUR"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ConvertCurrencyAsync(request));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithNullToCurrency_ThrowsArgumentException()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 100,
                FromCurrency = "USD",
                ToCurrency = null
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ConvertCurrencyAsync(request));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithZeroAmount_ThrowsArgumentException()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 0,
                FromCurrency = "USD",
                ToCurrency = "EUR"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ConvertCurrencyAsync(request));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithNegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = -100,
                FromCurrency = "USD",
                ToCurrency = "EUR"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ConvertCurrencyAsync(request));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithCachedData_ReturnsCachedData()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 100,
                FromCurrency = "USD",
                ToCurrency = "EUR"
            };

            var cachedResponse = new ExchangeRateResponse
            {
                Amount = 1,
                BaseCurrency = request.FromCurrency,
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { request.ToCurrency, 0.85m }
                }
            };

            var expectedResult = new CurrencyConversionResponse
            {
                Amount = request.Amount,
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                ConvertedAmount = 85.0m,
                Rate = 0.85m,
                Date = DateTime.Today
            };

            var cacheKey = CacheKeys.ConversionRate(request.FromCurrency, request.ToCurrency);
            _cache.Set(cacheKey, cachedResponse, TimeSpan.FromHours(1));

            _mockMapper.Setup(m => m.Map<CurrencyConversionResponse>(
                    It.Is<(ExchangeRateResponse Source, decimal Amount, string FromCurrency, string ToCurrency)>(
                        tuple => tuple.Source == cachedResponse &&
                                tuple.Amount == request.Amount &&
                                tuple.FromCurrency == request.FromCurrency &&
                                tuple.ToCurrency == request.ToCurrency)))
                .Returns(expectedResult);

            // Act
            var result = await _service.ConvertCurrencyAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResult, result);

            // Verify provider was not called
            _mockProvider.Verify(p => p.ConvertCurrencyAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.GetHistoricalRatesAsync(null));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithNullBaseCurrency_ThrowsArgumentException()
        {
            // Arrange
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = null,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 1, 5),
                Page = 1,
                PageSize = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetHistoricalRatesAsync(request));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithEndDateBeforeStartDate_ThrowsArgumentException()
        {
            // Arrange
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = new DateTime(2020, 1, 5),
                EndDate = new DateTime(2020, 1, 1),
                Page = 1,
                PageSize = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetHistoricalRatesAsync(request));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithNegativePage_SetsPageToOne()
        {
            // Arrange
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 1, 5),
                Page = -1,
                PageSize = 10
            };

            var historicalData = new Dictionary<DateTime, Dictionary<string, decimal>>
            {
                {
                    new DateTime(2020, 1, 1),
                    new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.75m } }
                }
            };

            var expectedResult = new PaginatedResponse<HistoricalRate>
            {
                Items = new List<HistoricalRate>(),
                Page = 1, // Should be corrected to 1
                PageSize = 10,
                TotalCount = 1
            };

            _mockProvider.Setup(p => p.GetHistoricalRatesAsync(
                    request.BaseCurrency, request.StartDate, request.EndDate))
                .ReturnsAsync(historicalData);

            _mockMapper.Setup(m => m.Map<PaginatedResponse<HistoricalRate>>(
                    It.Is<(HistoricalRatesRequest Request, List<HistoricalRate> AllRates)>(
                        tuple => tuple.Request.Page == 1))) // Check that page was corrected
                .Returns(expectedResult);

            // Act
            var result = await _service.GetHistoricalRatesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Page); // Page should be corrected to 1
        }
    }
}
