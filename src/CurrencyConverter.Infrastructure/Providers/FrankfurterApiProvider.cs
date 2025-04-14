using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace CurrencyConverter.Infrastructure.Providers
{
    public class FrankfurterApiProvider : ICurrencyProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FrankfurterApiProvider> _logger;
        private const string BaseUrl = "https://api.frankfurter.app";

        public string ProviderName => "Frankfurter";

        public FrankfurterApiProvider(HttpClient httpClient, ILogger<FrankfurterApiProvider> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
        {
            try
            {
                _logger.LogInformation("Fetching latest rates for base currency: {BaseCurrency}", baseCurrency);
                var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>($"/latest?from={baseCurrency}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest rates for base currency: {BaseCurrency}", baseCurrency);
                throw;
            }
        }

        public async Task<ExchangeRateResponse> ConvertCurrencyAsync(decimal amount, 
                                                                     string fromCurrency,
                                                                     string toCurrency)
        {
            try
            {
                _logger.LogInformation("Converting {Amount} from {FromCurrency} to {ToCurrency}", amount, fromCurrency, toCurrency);
                var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>($"/latest?amount={amount}&from={fromCurrency}&to={toCurrency}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting {Amount} from {FromCurrency} to {ToCurrency}", amount, fromCurrency, toCurrency);
                throw;
            }
        }

        public async Task<Dictionary<DateTime, Dictionary<string, decimal>>> 
            GetHistoricalRatesAsync(string baseCurrency,
                                    DateTime startDate,
                                    DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Fetching historical rates for base currency: {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));

                var url = $"/{startDate.ToString("yyyy-MM-dd")}..{endDate.ToString("yyyy-MM-dd")}?from={baseCurrency}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var historicalData = JsonSerializer.Deserialize<HistoricalRatesResponse>(content, options);
                return historicalData.Rates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical rates for base currency: {BaseCurrency} from {StartDate} to {EndDate}",
                    baseCurrency, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
                throw;
            }
        }

        private class HistoricalRatesResponse
        {
            public decimal Amount { get; set; }
            public string Base { get; set; }
            public DateTime Start_Date { get; set; }
            public DateTime End_Date { get; set; }
            public Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; }
        }
    }
}
