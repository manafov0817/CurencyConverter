using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.Http;
using Xunit;

namespace CurrencyConverter.IntegrationTests
{
    public class FrankfurterApiProviderIntegrationTests : IDisposable
    {
        private readonly FrankfurterApiProvider _provider;
        private readonly ILogger<FrankfurterApiProvider> _logger;
        private readonly HttpClient _httpClient;

        public FrankfurterApiProviderIntegrationTests()
        {
            _logger = NullLogger<FrankfurterApiProvider>.Instance;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.frankfurter.app")
            };
            _provider = new FrankfurterApiProvider(_httpClient, _logger);
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
            
            // Verify we got data for each day (excluding weekends and holidays)
            var businessDays = CountBusinessDays(startDate, endDate);
            Assert.True(result.Count <= businessDays);
            
            // Verify the structure of the returned data
            foreach (var date in result.Keys)
            {
                Assert.True(date >= startDate && date <= endDate);
                Assert.NotEmpty(result[date]);
            }
        }
        
        [Fact]
        public async Task GetLatestRatesAsync_WithInvalidBaseCurrency_ThrowsException()
        {
            // Arrange
            var baseCurrency = "INVALID";

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => 
                _provider.GetLatestRatesAsync(baseCurrency));
        }
        
        private int CountBusinessDays(DateTime startDate, DateTime endDate)
        {
            int days = 0;
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    days++;
                }
            }
            return days;
        }
    }
}
