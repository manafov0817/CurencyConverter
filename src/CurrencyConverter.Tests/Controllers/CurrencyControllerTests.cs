using CurrencyConverter.Api.Controllers;
using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models.Currency;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CurrencyConverter.Tests.Controllers
{
    public class CurrencyControllerTests
    {
        private readonly Mock<ICurrencyService> _mockCurrencyService;
        private readonly CurrencyController _controller;

        public CurrencyControllerTests()
        {
            _mockCurrencyService = new Mock<ICurrencyService>();
            _controller = new CurrencyController(_mockCurrencyService.Object);
        }

        [Fact]
        public async Task GetLatestRates_WithValidBaseCurrency_ReturnsOkWithRates()
        {
            // Arrange
            var baseCurrency = "USD";
            var expectedResponse = new ExchangeRateResponse
            {
                BaseCurrency = baseCurrency,
                Date = DateTime.Today,
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
            };

            _mockCurrencyService.Setup(s => s.GetLatestRatesAsync(baseCurrency))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetLatestRates(baseCurrency);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ExchangeRateResponse>(okResult.Value);
            Assert.Equal(expectedResponse.BaseCurrency, response.BaseCurrency);
            Assert.Equal(expectedResponse.Date, response.Date);
            Assert.Equal(expectedResponse.Rates, response.Rates);
        }

        [Fact]
        public async Task GetLatestRates_WithEmptyBaseCurrency_ReturnsBadRequest()
        {
            // Arrange
            string baseCurrency = "";

            // Act
            var result = await _controller.GetLatestRates(baseCurrency);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetLatestRates_WithNullBaseCurrency_ReturnsBadRequest()
        {
            // Arrange
            string baseCurrency = null;

            // Act
            var result = await _controller.GetLatestRates(baseCurrency);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConvertCurrency_WithValidParameters_ReturnsOkWithConversion()
        {
            // Arrange
            decimal amount = 100;
            string fromCurrency = "USD";
            string toCurrency = "EUR";

            var expectedResponse = new CurrencyConversionResponse
            {
                Amount = amount,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                ConvertedAmount = 85m,
                Rate = 0.85m
            };

            _mockCurrencyService.Setup(s => s.ConvertCurrencyAsync(It.IsAny<CurrencyConversionRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.ConvertCurrency(amount, fromCurrency, toCurrency);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<CurrencyConversionResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Amount, response.Amount);
            Assert.Equal(expectedResponse.FromCurrency, response.FromCurrency);
            Assert.Equal(expectedResponse.ToCurrency, response.ToCurrency);
            Assert.Equal(expectedResponse.ConvertedAmount, response.ConvertedAmount);
            Assert.Equal(expectedResponse.Rate, response.Rate);
        }

        [Fact]
        public async Task ConvertCurrency_WithZeroAmount_ReturnsBadRequest()
        {
            // Arrange
            decimal amount = 0;
            string fromCurrency = "USD";
            string toCurrency = "EUR";

            // Act
            var result = await _controller.ConvertCurrency(amount, fromCurrency, toCurrency);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConvertCurrency_WithNegativeAmount_ReturnsBadRequest()
        {
            // Arrange
            decimal amount = -100;
            string fromCurrency = "USD";
            string toCurrency = "EUR";

            // Act
            var result = await _controller.ConvertCurrency(amount, fromCurrency, toCurrency);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConvertCurrency_WithEmptyFromCurrency_ReturnsBadRequest()
        {
            // Arrange
            decimal amount = 100;
            string fromCurrency = "";
            string toCurrency = "EUR";

            // Act
            var result = await _controller.ConvertCurrency(amount, fromCurrency, toCurrency);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConvertCurrency_WithEmptyToCurrency_ReturnsBadRequest()
        {
            // Arrange
            decimal amount = 100;
            string fromCurrency = "USD";
            string toCurrency = "";

            // Act
            var result = await _controller.ConvertCurrency(amount, fromCurrency, toCurrency);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistoricalRates_WithValidParameters_ReturnsOkWithRates()
        {
            // Arrange
            string baseCurrency = "USD";
            DateTime startDate = new DateTime(2023, 1, 1);
            DateTime endDate = new DateTime(2023, 1, 31);
            int page = 1;
            int pageSize = 10;

            var historicalRates = new List<HistoricalRate>
            {
                new HistoricalRate
                {
                    Date = new DateTime(2023, 1, 1),
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
                }
            };

            var expectedResponse = new PaginatedResponse<HistoricalRate>
            {
                Items = historicalRates,
                Page = page,
                PageSize = pageSize,
                TotalCount = 1, 
            };

            _mockCurrencyService.Setup(s => s.GetHistoricalRatesAsync(It.IsAny<HistoricalRatesRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<PaginatedResponse<HistoricalRate>>(okResult.Value); 
            Assert.Equal(expectedResponse.Page, response.Page);
            Assert.Equal(expectedResponse.PageSize, response.PageSize);
            Assert.Equal(expectedResponse.TotalCount, response.TotalCount);
            Assert.Equal(expectedResponse.TotalPages, response.TotalPages);
        }

        [Fact]
        public async Task GetHistoricalRates_WithEmptyBaseCurrency_ReturnsBadRequest()
        {
            // Arrange
            string baseCurrency = "";
            DateTime startDate = new DateTime(2023, 1, 1);
            DateTime endDate = new DateTime(2023, 1, 31);

            // Act
            var result = await _controller.GetHistoricalRates(baseCurrency, startDate, endDate);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistoricalRates_WithStartDateAfterEndDate_ReturnsBadRequest()
        {
            // Arrange
            string baseCurrency = "USD";
            DateTime startDate = new DateTime(2023, 1, 31);
            DateTime endDate = new DateTime(2023, 1, 1);

            // Act
            var result = await _controller.GetHistoricalRates(baseCurrency, startDate, endDate);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task GetHistoricalRates_WithNegativePage_CorrectsPagination()
        {
            // Arrange
            string baseCurrency = "USD";
            DateTime startDate = new DateTime(2023, 1, 1);
            DateTime endDate = new DateTime(2023, 1, 31);
            int page = -1;
            int pageSize = 10;

            var historicalRates = new List<HistoricalRate>
            {
                new HistoricalRate
                {
                    Date = new DateTime(2023, 1, 1),
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
                }
            };

            var expectedResponse = new PaginatedResponse<HistoricalRate>
            {
                Items = historicalRates,
                Page = 1, // Should be corrected to 1
                PageSize = pageSize,
                TotalCount = 1
            };

            _mockCurrencyService.Setup(s => s.GetHistoricalRatesAsync(It.Is<HistoricalRatesRequest>(r => r.Page == 1)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<PaginatedResponse<HistoricalRate>>(okResult.Value);
        }

        [Fact]
        public async Task GetHistoricalRates_WithNegativePageSize_CorrectsPagination()
        {
            // Arrange
            string baseCurrency = "USD";
            DateTime startDate = new DateTime(2023, 1, 1);
            DateTime endDate = new DateTime(2023, 1, 31);
            int page = 1;
            int pageSize = -1;

            var historicalRates = new List<HistoricalRate>
            {
                new HistoricalRate
                {
                    Date = new DateTime(2023, 1, 1),
                    Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
                }
            };

            var expectedResponse = new PaginatedResponse<HistoricalRate>
            {
                Items = historicalRates,
                Page = page,
                PageSize = 10, // Should be corrected to 10
                TotalCount = 1
            };

            _mockCurrencyService.Setup(s => s.GetHistoricalRatesAsync(It.Is<HistoricalRatesRequest>(r => r.PageSize == 10)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<PaginatedResponse<HistoricalRate>>(okResult.Value);
        }
    }
}
