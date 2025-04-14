using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Core.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CurrencyConverter.Tests
{
    public class CurrencyServiceTests
    {
        private readonly Mock<ICurrencyProviderFactory> _mockProviderFactory;
        private readonly Mock<ICurrencyProvider> _mockProvider;
        private readonly Mock<ILogger<CurrencyService>> _mockLogger;
        private readonly IMemoryCache _cache;
        private readonly CurrencyService _service;

        public CurrencyServiceTests()
        {
            _mockProviderFactory = new Mock<ICurrencyProviderFactory>();
            _mockProvider = new Mock<ICurrencyProvider>();
            _mockLogger = new Mock<ILogger<CurrencyService>>();
            
            // Setup real memory cache for testing
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            // Setup provider factory to return our mock provider
            _mockProviderFactory.Setup(f => f.GetProvider(It.IsAny<string>()))
                .Returns(_mockProvider.Object);
            
            _service = new CurrencyService(_mockProviderFactory.Object, _cache, _mockLogger.Object);
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

            _mockProvider.Setup(p => p.ConvertCurrencyAsync(1, request.FromCurrency, request.ToCurrency))
                .ReturnsAsync(conversionResponse);

            // Act
            var result = await _service.ConvertCurrencyAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Amount, result.Amount);
            Assert.Equal(request.FromCurrency, result.FromCurrency);
            Assert.Equal(request.ToCurrency, result.ToCurrency);
            Assert.Equal(85.0m, result.ConvertedAmount); // 100 * 0.85
            Assert.Equal(0.85m, result.Rate);
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

            _mockProvider.Setup(p => p.GetHistoricalRatesAsync(
                    request.BaseCurrency, request.StartDate, request.EndDate))
                .ReturnsAsync(historicalData);

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
