using CurrencyConverter.Core.Models.Currency;
using Mapster;

namespace CurrencyConverter.Core.Mapping
{
    public class CurrencyMappingProfile : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<(ExchangeRateResponse Source, decimal Amount, string FromCurrency, string ToCurrency), CurrencyConversionResponse>()
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.FromCurrency, src => src.FromCurrency)
                .Map(dest => dest.ToCurrency, src => src.ToCurrency)
                .Map(dest => dest.ConvertedAmount, src => src.Amount * src.Source.Rates[src.ToCurrency])
                .Map(dest => dest.Rate, src => src.Source.Rates[src.ToCurrency])
                .Map(dest => dest.Date, src => src.Source.Date);

            config.NewConfig<ExchangeRateResponse, CurrencyConversionResponse>()
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.FromCurrency, src => src.BaseCurrency)
                .Map(dest => dest.ToCurrency, src => src.Rates.Keys.FirstOrDefault())
                .Map(dest => dest.ConvertedAmount, src => src.Amount * (src.Rates.Any() ? src.Rates.Values.FirstOrDefault() : 1))
                .Map(dest => dest.Rate, src => src.Rates.Any() ? src.Rates.Values.FirstOrDefault() : 1)
                .Map(dest => dest.Date, src => src.Date);

            config.NewConfig<CurrencyConversionRequest, ExchangeRateResponse>()
                .Map(dest => dest.Amount, src => src.Amount)
                .Map(dest => dest.BaseCurrency, src => src.FromCurrency)
                .Map(dest => dest.Date, _ => DateTime.UtcNow)
                .Map(dest => dest.Rates, src => new Dictionary<string, decimal> { { src.ToCurrency, 1.0m } });

            config.NewConfig<(Dictionary<DateTime, Dictionary<string, decimal>> Data, string BaseCurrency), List<HistoricalRate>>()
                .MapWith(src => src.Data.Select(kvp => new HistoricalRate
                {
                    Date = kvp.Key,
                    BaseCurrency = src.BaseCurrency,
                    Rates = kvp.Value
                }).ToList());

            config.NewConfig<(HistoricalRatesRequest Request, List<HistoricalRate> AllRates), PaginatedResponse<HistoricalRate>>()
                .MapWith(src => new PaginatedResponse<HistoricalRate>
                {
                    Items = src.AllRates
                        .Skip((src.Request.Page - 1) * src.Request.PageSize)
                        .Take(src.Request.PageSize)
                        .ToList(),
                    Page = src.Request.Page,
                    PageSize = src.Request.PageSize,
                    TotalCount = src.AllRates.Count
                });

            config.NewConfig<(CurrencyConversionRequest Request, decimal Rate, decimal ConvertedAmount, DateTime Date), CurrencyConversionResponse>()
                .Map(dest => dest.Amount, src => src.Request.Amount)
                .Map(dest => dest.FromCurrency, src => src.Request.FromCurrency)
                .Map(dest => dest.ToCurrency, src => src.Request.ToCurrency)
                .Map(dest => dest.ConvertedAmount, src => src.ConvertedAmount)
                .Map(dest => dest.Rate, src => src.Rate)
                .Map(dest => dest.Date, src => src.Date);
        }
    }
}
