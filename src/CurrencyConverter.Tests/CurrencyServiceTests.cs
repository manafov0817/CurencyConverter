using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models.Currency;
using CurrencyConverter.Core.Services;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CurrencyConverter.Tests
{
    public class CurrencyServiceTests
    {
        private readonly Mock<ICurrencyProviderFactory> _mockProviderFactory;
        private readonly Mock<ICurrencyProvider> _mockProvider;
        private readonly Mock<ILogger<CurrencyService>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly IMemoryCache _cache;
        private readonly CurrencyService _service;

        public CurrencyServiceTests()
        {
            _mockProviderFactory = new Mock<ICurrencyProviderFactory>();
            _mockProvider = new Mock<ICurrencyProvider>();
            _mockLogger = new Mock<ILogger<CurrencyService>>();
            _mockMapper = new Mock<IMapper>();

            // Setup real memory cache for testing
            _cache = new MemoryCache(new MemoryCacheOptions());

            // Setup provider factory to return our mock provider
            _mockProviderFactory.Setup(f => f.GetProvider(It.IsAny<string>()))
                .Returns(_mockProvider.Object);

            _service = new CurrencyService(_mockProviderFactory.Object, _cache, _mockLogger.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithValidBaseCurrency_ReturnsRates()
        {
            // Arrange
            var baseCurrency = "USD";
            var expectedResponse = new ExchangeRateResponse
            {
                Amount = 1,
                BaseCurrency = baseCurrency,
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m },
                    { "GBP", 0.75m },
                    { "JPY", 110.0m },
                    { "TRY", 8.5m } // This will be filtered out
                }
            };

            _mockProvider.Setup(p => p.GetLatestRatesAsync(baseCurrency))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _service.GetLatestRatesAsync(baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(baseCurrency, result.BaseCurrency);
            Assert.Equal(3, result.Rates.Count); // TRY should be filtered out
            Assert.False(result.Rates.ContainsKey("TRY"));
            Assert.Equal(0.85m, result.Rates["EUR"]);
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithRestrictedCurrency_ThrowsException()
        {
            // Arrange
            var baseCurrency = "TRY"; // Restricted currency

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetLatestRatesAsync(baseCurrency));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithValidRequest_ReturnsConversion()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 100,
                FromCurrency = "USD",
                ToCurrency = "EUR"
            };

            var conversionResponse = new ExchangeRateResponse
            {
                Amount = 1,
                BaseCurrency = "USD",
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { "EUR", 0.85m }
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

            _mockProvider.Setup(p => p.ConvertCurrencyAsync(1, request.FromCurrency, request.ToCurrency))
                .ReturnsAsync(conversionResponse);

            // Setup mapper to return our expected result
            _mockMapper.Setup(m => m.Map<CurrencyConversionResponse>(
                    It.Is<(ExchangeRateResponse Source, decimal Amount, string FromCurrency, string ToCurrency)>(
                        tuple => tuple.Source == conversionResponse &&
                                tuple.Amount == request.Amount &&
                                tuple.FromCurrency == request.FromCurrency &&
                                tuple.ToCurrency == request.ToCurrency)))
                .Returns(expectedResult);

            // Act
            var result = await _service.ConvertCurrencyAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Amount, result.Amount);
            Assert.Equal(request.FromCurrency, result.FromCurrency);
            Assert.Equal(request.ToCurrency, result.ToCurrency);
            Assert.Equal(85.0m, result.ConvertedAmount); // 100 * 0.85
            Assert.Equal(0.85m, result.Rate);
            
            // Verify mapper was called
            _mockMapper.Verify(m => m.Map<CurrencyConversionResponse>(
                It.Is<(ExchangeRateResponse Source, decimal Amount, string FromCurrency, string ToCurrency)>(
                    tuple => tuple.Source == conversionResponse &&
                            tuple.Amount == request.Amount &&
                            tuple.FromCurrency == request.FromCurrency &&
                            tuple.ToCurrency == request.ToCurrency)), 
                Times.Once);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithRestrictedCurrency_ThrowsException()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 100,
                FromCurrency = "USD",
                ToCurrency = "TRY" // Restricted currency
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ConvertCurrencyAsync(request));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithValidRequest_ReturnsPaginatedResult()
        {
            // Arrange
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 1, 5),
                Page = 1,
                PageSize = 2
            };

            var historicalData = new Dictionary<DateTime, Dictionary<string, decimal>>
            {
                {
                    new DateTime(2020, 1, 1),
                    new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.75m }, { "TRY", 8.5m } }
                },
                {
                    new DateTime(2020, 1, 2),
                    new Dictionary<string, decimal> { { "EUR", 0.86m }, { "GBP", 0.76m }, { "TRY", 8.6m } }
                },
                {
                    new DateTime(2020, 1, 3),
                    new Dictionary<string, decimal> { { "EUR", 0.87m }, { "GBP", 0.77m }, { "TRY", 8.7m } }
                }
            };

            var historicalRates = new List<HistoricalRate>
            {
                new HistoricalRate
                {
                    Date = new DateTime(2020, 1, 1),
                    BaseCurrency = request.BaseCurrency,
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.75m } }
                },
                new HistoricalRate
                {
                    Date = new DateTime(2020, 1, 2),
                    BaseCurrency = request.BaseCurrency,
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.86m }, { "GBP", 0.76m } }
                }
            };

            var expectedResult = new PaginatedResponse<HistoricalRate>
            {
                Items = historicalRates,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = 3
            };

            _mockProvider.Setup(p => p.GetHistoricalRatesAsync(
                    request.BaseCurrency, request.StartDate, request.EndDate))
                .ReturnsAsync(historicalData);

            // Setup mapper to return our expected result
            _mockMapper.Setup(m => m.Map<List<HistoricalRate>>(
                    It.Is<(Dictionary<DateTime, Dictionary<string, decimal>> Data, string BaseCurrency)>(
                        tuple => tuple.BaseCurrency == request.BaseCurrency)))
                .Returns(historicalData.Select(kvp => new HistoricalRate
                {
                    Date = kvp.Key,
                    BaseCurrency = request.BaseCurrency,
                    Rates = kvp.Value.Where(r => !_service.IsRestrictedCurrency(r.Key))
                        .ToDictionary(r => r.Key, r => r.Value)
                }).ToList());

            _mockMapper.Setup(m => m.Map<PaginatedResponse<HistoricalRate>>(
                    It.Is<(HistoricalRatesRequest Request, List<HistoricalRate> AllRates)>(
                        tuple => tuple.Request == request)))
                .Returns(expectedResult);

            // Act
            var result = await _service.GetHistoricalRatesAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Page, result.Page);
            Assert.Equal(request.PageSize, result.PageSize);
            Assert.Equal(3, result.TotalCount);
            Assert.Equal(2, result.Items.Count());

            // Verify restricted currencies are filtered out
            foreach (var item in result.Items)
            {
                Assert.False(item.Rates.ContainsKey("TRY"));
                Assert.Equal(2, item.Rates.Count); // Should have EUR and GBP, but not TRY
            }
            
            // Verify mapper was called
            _mockMapper.Verify(m => m.Map<PaginatedResponse<HistoricalRate>>(
                It.Is<(HistoricalRatesRequest Request, List<HistoricalRate> AllRates)>(
                    tuple => tuple.Request == request)), 
                Times.Once);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithRestrictedCurrency_ThrowsException()
        {
            // Arrange
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "PLN", // Restricted currency
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2020, 1, 5),
                Page = 1,
                PageSize = 10
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GetHistoricalRatesAsync(request));
        }
    }
}
