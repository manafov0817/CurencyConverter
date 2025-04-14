using CurrencyConverter.Core.Models.Currency;
using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Tests.Providers
{
    public class FrankfurterApiProviderEdgeCaseTests
    {
        private readonly Mock<ILogger<FrankfurterApiProvider>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly FrankfurterApiProvider _provider;

        public FrankfurterApiProviderEdgeCaseTests()
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
        public void ProviderName_ReturnsCorrectValue()
        {
            // Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.Equal("Frankfurter", providerName);
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new FrankfurterApiProvider(null, _mockLogger.Object));
            
            Assert.Equal("httpClient", exception.ParamName);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                new FrankfurterApiProvider(_httpClient, null));
            
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithEmptyBaseCurrency_StillMakesRequest()
        {
            // Arrange
            var baseCurrency = string.Empty;
            var responseData = new ExchangeRateResponse
            {
                Amount = 1,
                BaseCurrency = "EUR", // Default base currency when empty
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { "USD", 1.1m }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(responseData);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get &&
                        req.RequestUri.ToString().Contains("/latest?from=")),
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
            Assert.Equal("EUR", result.BaseCurrency);
            Assert.Equal(1, result.Rates.Count);
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithZeroAmount_ReturnsZeroRates()
        {
            // Arrange
            var amount = 0m;
            var fromCurrency = "USD";
            var toCurrency = "EUR";

            var responseData = new ExchangeRateResponse
            {
                Amount = amount,
                BaseCurrency = fromCurrency,
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal>
                {
                    { toCurrency, 0m }
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
            Assert.Equal(0m, result.Amount);
            Assert.Equal(0m, result.Rates[toCurrency]);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithSameDates_ReturnsDataForSingleDay()
        {
            // Arrange
            var baseCurrency = "USD";
            var sameDate = new DateTime(2020, 1, 1);

            var responseData = new
            {
                amount = 1,
                @base = baseCurrency,
                start_date = sameDate.ToString("yyyy-MM-dd"),
                end_date = sameDate.ToString("yyyy-MM-dd"),
                rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    {
                        "2020-01-01",
                        new Dictionary<string, decimal> { { "EUR", 0.85m } }
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
                        req.RequestUri.ToString().Contains($"/{sameDate.ToString("yyyy-MM-dd")}..{sameDate.ToString("yyyy-MM-dd")}?from={baseCurrency}")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                });

            // Act
            var result = await _provider.GetHistoricalRatesAsync(baseCurrency, sameDate, sameDate);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result.ContainsKey(sameDate));
            Assert.Equal(0.85m, result[sameDate]["EUR"]);
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithEmptyResponse_ReturnsEmptyDictionary()
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
                rates = new Dictionary<string, Dictionary<string, decimal>>()
            };

            var jsonResponse = JsonSerializer.Serialize(responseData);

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
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
            Assert.Empty(result);
        }
    }
}
