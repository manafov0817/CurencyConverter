namespace CurrencyConverter.Core.Models.Currency
{
    public class HistoricalRate
    {
        public DateTime Date { get; set; }
        public string BaseCurrency { get; set; } = string.Empty;
        public Dictionary<string, decimal> Rates { get; set; } = new Dictionary<string, decimal>();
    }
}
