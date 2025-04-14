namespace CurrencyConverter.Core.Models.Currency
{
    public class CurrencyConversionResponse
    {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public decimal ConvertedAmount { get; set; }
        public DateTime Date { get; set; }
        public decimal Rate { get; set; }
    }
}
