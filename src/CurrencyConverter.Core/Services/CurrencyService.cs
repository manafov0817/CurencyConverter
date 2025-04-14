using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models.Currency;
using CurrencyConverter.Core.Utilities;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Core.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly ICurrencyProviderFactory _providerFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CurrencyService> _logger;
        private readonly IMapper _mapper;
        private readonly HashSet<string> _restrictedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TRY", "PLN", "THB", "MXN"
        };

        public CurrencyService(
            ICurrencyProviderFactory providerFactory,
            IMemoryCache cache,
            ILogger<CurrencyService> logger,
            IMapper mapper)
        {
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public bool IsRestrictedCurrency(string currency)
        {
            return _restrictedCurrencies.Contains(currency);
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
        {
            if (string.IsNullOrEmpty(baseCurrency))
            {
                throw new ArgumentException("Base currency cannot be null or empty", nameof(baseCurrency));
            }

            if (_restrictedCurrencies.Contains(baseCurrency))
            {
                throw new InvalidOperationException($"Currency {baseCurrency} is restricted and cannot be used");
            }

            var cacheKey = CacheKeys.LatestRates(baseCurrency);
            if (!_cache.TryGetValue(cacheKey, out ExchangeRateResponse rates))
            {
                _logger.LogInformation("Cache miss for latest rates with base currency {BaseCurrency}", baseCurrency);

                var provider = _providerFactory.GetProvider();
                rates = await provider.GetLatestRatesAsync(baseCurrency);

                rates.Rates.Remove("TRY");
                rates.Rates.Remove("PLN");
                rates.Rates.Remove("THB");
                rates.Rates.Remove("MXN");

                _cache.Set(cacheKey, rates, TimeSpan.FromHours(1));
            }
            else
            {
                _logger.LogInformation("Cache hit for latest rates with base currency {BaseCurrency}", baseCurrency);
            }

            return rates;
        }

        public async Task<CurrencyConversionResponse> ConvertCurrencyAsync(CurrencyConversionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.FromCurrency) || string.IsNullOrEmpty(request.ToCurrency))
            {
                throw new ArgumentException("Source and target currencies must be specified");
            }

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero", nameof(request.Amount));
            }

            if (_restrictedCurrencies.Contains(request.FromCurrency) || _restrictedCurrencies.Contains(request.ToCurrency))
            {
                throw new InvalidOperationException($"Restricted currencies cannot be used in conversion");
            }

            var provider = _providerFactory.GetProvider();
            var cacheKey = CacheKeys.ConversionRate(request.FromCurrency, request.ToCurrency);

            if (!_cache.TryGetValue(cacheKey, out ExchangeRateResponse conversionData))
            {
                _logger.LogInformation("Cache miss for conversion from {FromCurrency} to {ToCurrency}",
                    request.FromCurrency, request.ToCurrency);

                conversionData = await provider.ConvertCurrencyAsync(1, request.FromCurrency, request.ToCurrency);

                _cache.Set(cacheKey, conversionData, TimeSpan.FromHours(1));
            }
            else
            {
                _logger.LogInformation("Cache hit for conversion from {FromCurrency} to {ToCurrency}",
                    request.FromCurrency, request.ToCurrency);
            }

            return _mapper.Map<CurrencyConversionResponse>((
                Source: conversionData,
                Amount: request.Amount,
                FromCurrency: request.FromCurrency,
                ToCurrency: request.ToCurrency
            ));
        }

        public async Task<PaginatedResponse<HistoricalRate>> GetHistoricalRatesAsync(HistoricalRatesRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.BaseCurrency))
            {
                throw new ArgumentException("Base currency must be specified", nameof(request.BaseCurrency));
            }

            if (_restrictedCurrencies.Contains(request.BaseCurrency))
            {
                throw new InvalidOperationException($"Currency {request.BaseCurrency} is restricted and cannot be used");
            }

            if (request.StartDate > request.EndDate)
            {
                throw new ArgumentException("Start date must be before or equal to end date");
            }

            if (request.Page < 1)
            {
                request.Page = 1;
            }

            if (request.PageSize < 1)
            {
                request.PageSize = 10;
            }

            var cacheKey = CacheKeys.HistoricalRates(
                request.BaseCurrency,
                request.StartDate.ToString("yyyy-MM-dd"),
                request.EndDate.ToString("yyyy-MM-dd"));

            if (!_cache.TryGetValue(cacheKey, out Dictionary<DateTime, Dictionary<string, decimal>> historicalData))
            {
                _logger.LogInformation("Cache miss for historical rates with base currency {BaseCurrency} from {StartDate} to {EndDate}",
                    request.BaseCurrency, request.StartDate.ToString("yyyy-MM-dd"), request.EndDate.ToString("yyyy-MM-dd"));

                var provider = _providerFactory.GetProvider();
                historicalData = await provider.GetHistoricalRatesAsync(request.BaseCurrency, request.StartDate, request.EndDate);

                _cache.Set(cacheKey, historicalData, TimeSpan.FromHours(24));
            }
            else
            {
                _logger.LogInformation("Cache hit for historical rates with base currency {BaseCurrency} from {StartDate} to {EndDate}",
                    request.BaseCurrency, request.StartDate.ToString("yyyy-MM-dd"), request.EndDate.ToString("yyyy-MM-dd"));
            }

            foreach (var date in historicalData.Keys)
            {
                historicalData[date].Remove("TRY");
                historicalData[date].Remove("PLN");
                historicalData[date].Remove("THB");
                historicalData[date].Remove("MXN");
            }

            var allRates = historicalData
                .Select(kvp => new HistoricalRate
                {
                    Date = kvp.Key,
                    BaseCurrency = request.BaseCurrency,
                    Rates = kvp.Value
                })
                .OrderByDescending(r => r.Date)
                .ToList();

            var totalCount = allRates.Count;
            var skip = (request.Page - 1) * request.PageSize;
            var paginatedRates = allRates
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();

            return new PaginatedResponse<HistoricalRate>
            {
                Items = paginatedRates,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount 
            };
        }
    }
}
