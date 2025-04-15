using CurrencyConverter.Core.Models.Currency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace CurrencyConverter.Tests.Models.Currency
{
    public class CurrencyModelsTests
    {
        [Fact]
        public void HistoricalRate_Properties_WorkAsExpected()
        {
            // Arrange
            var date = DateTime.Today;
            var rates = new Dictionary<string, decimal> { { "USD", 1.2m }, { "EUR", 0.85m } };
            var historicalRate = new HistoricalRate
            {
                Date = date,
                BaseCurrency = "GBP",
                Rates = rates
            };

            // Assert
            Assert.Equal(date, historicalRate.Date);
            Assert.Equal("GBP", historicalRate.BaseCurrency);
            Assert.Equal(rates, historicalRate.Rates);
            Assert.Equal(1.2m, historicalRate.Rates["USD"]);
            Assert.Equal(0.85m, historicalRate.Rates["EUR"]);
        }

        [Fact]
        public void HistoricalRate_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var historicalRate = new HistoricalRate();

            // Assert
            Assert.Equal(default, historicalRate.Date);
            Assert.Equal(string.Empty, historicalRate.BaseCurrency);
            Assert.NotNull(historicalRate.Rates);
            Assert.Empty(historicalRate.Rates);
        }

        [Fact]
        public void CurrencyConversionRequest_Properties_WorkAsExpected()
        {
            // Arrange
            var request = new CurrencyConversionRequest
            {
                Amount = 100m,
                FromCurrency = "USD",
                ToCurrency = "EUR"
            };

            // Assert
            Assert.Equal(100m, request.Amount);
            Assert.Equal("USD", request.FromCurrency);
            Assert.Equal("EUR", request.ToCurrency);
        }

        [Fact]
        public void CurrencyConversionRequest_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var request = new CurrencyConversionRequest();

            // Assert
            Assert.Equal(0m, request.Amount);
            Assert.Equal(string.Empty, request.FromCurrency);
            Assert.Equal(string.Empty, request.ToCurrency);
        }

        [Fact]
        public void CurrencyConversionResponse_Properties_WorkAsExpected()
        {
            // Arrange
            var date = DateTime.Today;
            var response = new CurrencyConversionResponse
            {
                Amount = 100m,
                FromCurrency = "USD",
                ToCurrency = "EUR",
                ConvertedAmount = 85m,
                Date = date,
                Rate = 0.85m
            };

            // Assert
            Assert.Equal(100m, response.Amount);
            Assert.Equal("USD", response.FromCurrency);
            Assert.Equal("EUR", response.ToCurrency);
            Assert.Equal(85m, response.ConvertedAmount);
            Assert.Equal(date, response.Date);
            Assert.Equal(0.85m, response.Rate);
        }

        [Fact]
        public void CurrencyConversionResponse_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var response = new CurrencyConversionResponse();

            // Assert
            Assert.Equal(0m, response.Amount);
            Assert.Equal(string.Empty, response.FromCurrency);
            Assert.Equal(string.Empty, response.ToCurrency);
            Assert.Equal(0m, response.ConvertedAmount);
            Assert.Equal(default, response.Date);
            Assert.Equal(0m, response.Rate);
        }

        [Fact]
        public void HistoricalRatesRequest_Properties_WorkAsExpected()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);
            var request = new HistoricalRatesRequest
            {
                BaseCurrency = "USD",
                StartDate = startDate,
                EndDate = endDate,
                Page = 2,
                PageSize = 20
            };

            // Assert
            Assert.Equal("USD", request.BaseCurrency);
            Assert.Equal(startDate, request.StartDate);
            Assert.Equal(endDate, request.EndDate);
            Assert.Equal(2, request.Page);
            Assert.Equal(20, request.PageSize);
        }

        [Fact]
        public void HistoricalRatesRequest_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var request = new HistoricalRatesRequest();

            // Assert
            Assert.Equal(string.Empty, request.BaseCurrency);
            Assert.Equal(default, request.StartDate);
            Assert.Equal(default, request.EndDate);
            Assert.Equal(1, request.Page);
            Assert.Equal(10, request.PageSize);
        }

        [Fact]
        public void ExchangeRateResponse_Properties_WorkAsExpected()
        {
            // Arrange
            var date = DateTime.Today;
            var rates = new Dictionary<string, decimal> { { "USD", 1.2m }, { "EUR", 0.85m } };
            var response = new ExchangeRateResponse
            {
                Amount = 1m,
                BaseCurrency = "GBP",
                Date = date,
                Rates = rates
            };

            // Assert
            Assert.Equal(1m, response.Amount);
            Assert.Equal("GBP", response.BaseCurrency);
            Assert.Equal(date, response.Date);
            Assert.Equal(rates, response.Rates);
            Assert.Equal(1.2m, response.Rates["USD"]);
            Assert.Equal(0.85m, response.Rates["EUR"]);
        }

        [Fact]
        public void ExchangeRateResponse_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var response = new ExchangeRateResponse();

            // Assert
            Assert.Equal(0m, response.Amount);
            Assert.Equal(string.Empty, response.BaseCurrency);
            Assert.Equal(default, response.Date);
            Assert.NotNull(response.Rates);
            Assert.Empty(response.Rates);
        }

        [Fact]
        public void PaginatedResponse_Properties_WorkAsExpected()
        {
            // Arrange
            var items = new List<string> { "Item1", "Item2", "Item3" };
            var response = new PaginatedResponse<string>
            {
                Items = items,
                TotalCount = 10,
                Page = 1,
                PageSize = 3
            };

            // Assert
            Assert.Equal(items, response.Items);
            Assert.Equal(10, response.TotalCount);
            Assert.Equal(1, response.Page);
            Assert.Equal(3, response.PageSize);
            Assert.Equal(4, response.TotalPages); // 10 items / 3 per page = 4 pages (ceiling)
        }

        [Fact]
        public void PaginatedResponse_DefaultValues_AreInitialized()
        {
            // Arrange & Act
            var response = new PaginatedResponse<string>();

            // Assert
            Assert.NotNull(response.Items);
            Assert.Empty(response.Items);
            Assert.Equal(0, response.TotalCount);
            Assert.Equal(0, response.Page);
            Assert.Equal(0, response.PageSize);
            Assert.Equal(0, response.TotalPages); // 0 / 0 would throw, but the method handles it
        }

        [Fact]
        public void PaginatedResponse_TotalPages_CalculatesCorrectly()
        {
            // Test different combinations of TotalCount and PageSize
            
            // Case 1: Exact division
            var response1 = new PaginatedResponse<string>
            {
                TotalCount = 10,
                PageSize = 5
            };
            Assert.Equal(2, response1.TotalPages);
            
            // Case 2: Partial page
            var response2 = new PaginatedResponse<string>
            {
                TotalCount = 11,
                PageSize = 5
            };
            Assert.Equal(3, response2.TotalPages);
            
            // Case 3: Zero items
            var response3 = new PaginatedResponse<string>
            {
                TotalCount = 0,
                PageSize = 5
            };
            Assert.Equal(0, response3.TotalPages);
            
            // Case 4: One item
            var response4 = new PaginatedResponse<string>
            {
                TotalCount = 1,
                PageSize = 5
            };
            Assert.Equal(1, response4.TotalPages);
        }
    }
}
