using System.Text.Json.Serialization;

namespace CurrencyConverter.Core.Models
{
    public class ExchangeRateResponse
    {
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("base")]
        public string BaseCurrency { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; }
    }

    public class CurrencyConversionRequest
    {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
    }

    public class CurrencyConversionResponse
    {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal ConvertedAmount { get; set; }
        public DateTime Date { get; set; }
        public decimal Rate { get; set; }
    }

    public class HistoricalRatesRequest
    {
        public string BaseCurrency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class HistoricalRate
    {
        public DateTime Date { get; set; }
        public string BaseCurrency { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
