using CurrencyConverter.Infrastructure.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Tests.Providers
{
    public class FrankfurterApiProviderAdditionalTests
    {
        private readonly Mock<ILogger<FrankfurterApiProvider>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly FrankfurterApiProvider _provider;

        public FrankfurterApiProviderAdditionalTests()
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
        public async Task ConvertCurrencyAsync_WhenApiCallFails_ThrowsException()
        {
            // Arrange
            var amount = 100m;
            var fromCurrency = "USD";
            var toCurrency = "EUR";

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
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _provider.ConvertCurrencyAsync(amount, fromCurrency, toCurrency));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WhenApiCallFails_ThrowsException()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = new DateTime(2020, 1, 1);
            var endDate = new DateTime(2020, 1, 5);

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
            await Assert.ThrowsAsync<HttpRequestException>(
                () => _provider.GetHistoricalRatesAsync(baseCurrency, startDate, endDate));
        }

        [Fact]
        public async Task GetLatestRatesAsync_WithInvalidJsonResponse_ThrowsException()
        {
            // Arrange
            var baseCurrency = "USD";
            var invalidJson = "{ invalid json }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(invalidJson)
                });

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(
                () => _provider.GetLatestRatesAsync(baseCurrency));
        }

        [Fact]
        public async Task ConvertCurrencyAsync_WithInvalidJsonResponse_ThrowsException()
        {
            // Arrange
            var amount = 100m;
            var fromCurrency = "USD";
            var toCurrency = "EUR";
            var invalidJson = "{ invalid json }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(invalidJson)
                });

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(
                () => _provider.ConvertCurrencyAsync(amount, fromCurrency, toCurrency));
        }

        [Fact]
        public async Task GetHistoricalRatesAsync_WithInvalidJsonResponse_ThrowsException()
        {
            // Arrange
            var baseCurrency = "USD";
            var startDate = new DateTime(2020, 1, 1);
            var endDate = new DateTime(2020, 1, 5);
            var invalidJson = "{ invalid json }";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(invalidJson)
                });

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(
                () => _provider.GetHistoricalRatesAsync(baseCurrency, startDate, endDate));
        }
    }
}
