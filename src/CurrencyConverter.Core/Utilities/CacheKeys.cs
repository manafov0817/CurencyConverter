namespace CurrencyConverter.Core.Utilities
{
    public static class CacheKeys
    {
        public static string LatestRates(string baseCurrency) => $"LatestRates_{baseCurrency}";
        public static string ConversionRate(string fromCurrency, string toCurrency) => $"ConversionRate_{fromCurrency}_{toCurrency}";
        public static string HistoricalRates(string baseCurrency, string startDate, string endDate) => $"HistoricalRates_{baseCurrency}_{startDate}_{endDate}";
    }
}
