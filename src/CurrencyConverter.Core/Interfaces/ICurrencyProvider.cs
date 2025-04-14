using CurrencyConverter.Core.Models.Currency;

namespace CurrencyConverter.Core.Interfaces
{
    public interface ICurrencyProvider
    {
        string ProviderName { get; }
        Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency);
        Task<ExchangeRateResponse> ConvertCurrencyAsync(decimal amount, string fromCurrency, string toCurrency);
        Task<Dictionary<DateTime, Dictionary<string, decimal>>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate);
    }
}
