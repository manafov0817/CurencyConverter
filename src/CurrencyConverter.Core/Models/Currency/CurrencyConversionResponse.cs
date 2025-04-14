using System;

namespace CurrencyConverter.Core.Models.Currency
{
    public record CurrencyConversionResponse
    {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal ConvertedAmount { get; set; }
        public DateTime Date { get; set; }
        public decimal Rate { get; set; }
    }
}
