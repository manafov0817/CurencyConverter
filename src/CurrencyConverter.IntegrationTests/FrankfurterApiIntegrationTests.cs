using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace CurrencyConverter.IntegrationTests
{
    public class FrankfurterApiIntegrationTests : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly FrankfurterApiProvider _provider;

        public FrankfurterApiIntegrationTests()
        {
            // Create a real HTTP client to test against the actual Frankfurter API
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.frankfurter.app")
            };
            
            var logger = NullLogger<FrankfurterApiProvider>.Instance;
            _provider = new FrankfurterApiProvider(_httpClient, logger);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithValidBaseCurrency_ReturnsRates()
        {
            // Arrange
            var baseCurrency = "USD";

            // Act
            var result = await _provider.GetLatestRatesAsync(baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(baseCurrency, result.BaseCurrency);
            Assert.NotEmpty(result.Rates);
            Assert.True(result.Rates.Count > 0);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithValidRequest_ReturnsConversion()
        {
            // Arrange
            var amount = 100m;
            var fromCurrency = "USD";
            var toCurrency = "EUR";

            // Act
            var result = await _provider.ConvertCurrencyAsync(amount, fromCurrency, toCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(amount, result.Amount);
            Assert.Equal(fromCurrency, result.BaseCurrency);
            Assert.True(result.Rates.ContainsKey(toCurrency));
            Assert.True(result.Rates[toCurrency] > 0);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithValidRequest_ReturnsHistoricalData()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = DateTime.Today.AddDays(-5);
            var endDate = DateTime.Today;

            // Act
            var result = await _provider.GetHistoricalRatesAsync(baseCurrency, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Verify the structure of the returned data
            foreach (var date in result.Keys)
            {
                Assert.True(date >= startDate && date <= endDate);
                Assert.NotEmpty(result[date]);
            }
        }
    }
}
