namespace CurrencyConverter.Core.Models.Currency
{
    public record HistoricalRate
    {
        public DateTime Date { get; set; }
        public string BaseCurrency { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
