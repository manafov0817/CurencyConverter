using CurrencyConverter.Core.Models;
using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Tests
{
    public class FrankfurterApiProviderTests
    {
        private readonly Mock<ILogger<FrankfurterApiProvider>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly FrankfurterApiProvider _provider;

        public FrankfurterApiProviderTests()
        {
            _mockLogger = new Mock<ILogger<FrankfurterApiProvider>>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://api.frankfurter.app")
            };

            _provider = new FrankfurterApiProvider(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task GetLatestRatesAsync_ReturnsCorrectData()
        {
            // Arrange
            var baseCurrency = "USD";
            var responseData = new ExchangeRateResponse
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

            var jsonResponse = JsonSerializer.Serialize(responseData);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().Contains($"/latest?from={baseCurrency}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _provider.GetLatestRatesAsync(baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(baseCurrency, result.BaseCurrency);
            Assert.Equal(2, result.Rates.Count);
            Assert.Equal(0.85m, result.Rates["EUR"]);
            Assert.Equal(0.75m, result.Rates["GBP"]);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_ReturnsCorrectConversion()
        {
            // Arrange
            var amount = 100m;
            var fromCurrency = "USD";
            var toCurrency = "EUR";

            var responseData = new ExchangeRateResponse
            {
                Amount = amount,
                BaseCurrency = fromCurrency,
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { toCurrency, 0.85m }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(responseData);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().Contains($"/latest?amount={amount}&from={fromCurrency}&to={toCurrency}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _provider.ConvertCurrencyAsync(amount, fromCurrency, toCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(amount, result.Amount);
            Assert.Equal(fromCurrency, result.BaseCurrency);
            Assert.Equal(1, result.Rates.Count);
            Assert.Equal(0.85m, result.Rates[toCurrency]);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_ReturnsCorrectData()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = new DateTime(2020, 1, 1);
            var endDate = new DateTime(2020, 1, 5);

            var responseData = new
            {
                amount = 1,
                @base = baseCurrency,
                start_date = startDate.ToString("yyyy-MM-dd"),
                end_date = endDate.ToString("yyyy-MM-dd"),
                rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    {
                        "2020-01-01",
                        new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.75m } }
                    },
                    {
                        "2020-01-02",
                        new Dictionary<string, decimal> { { "EUR", 0.86m }, { "GBP", 0.76m } }
                    }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(responseData);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().Contains($"/{startDate.ToString("yyyy-MM-dd")}..{endDate.ToString("yyyy-MM-dd")}?from={baseCurrency}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _provider.GetHistoricalRatesAsync(baseCurrency, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);

            // Verify the dates are parsed correctly
            Assert.True(result.ContainsKey(new DateTime(2020, 1, 1)));
            Assert.True(result.ContainsKey(new DateTime(2020, 1, 2)));

            // Verify the rates are correct
            Assert.Equal(0.85m, result[new DateTime(2020, 1, 1)]["EUR"]);
            Assert.Equal(0.75m, result[new DateTime(2020, 1, 1)]["GBP"]);
            Assert.Equal(0.86m, result[new DateTime(2020, 1, 2)]["EUR"]);
            Assert.Equal(0.76m, result[new DateTime(2020, 1, 2)]["GBP"]);
        }

        [Fact]
        public async Task GetLatestRatesAsync_WhenApiCallFails_ThrowsException()
        {
            // Arrange
            var baseCurrency = "USD";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError
                });

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                _provider.GetLatestRatesAsync(baseCurrency));
        }
    }
}
