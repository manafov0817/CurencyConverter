namespace CurrencyConverter.Core.Models.Currency
{
    public class HistoricalRatesRequest
    {
        public string BaseCurrency { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
