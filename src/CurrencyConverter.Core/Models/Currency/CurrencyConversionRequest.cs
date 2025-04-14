using System;

namespace CurrencyConverter.Core.Models.Currency
{
    public record CurrencyConversionRequest
    {
        public decimal Amount { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
    }
}
