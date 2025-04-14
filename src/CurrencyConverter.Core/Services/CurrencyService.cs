using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models;
using CurrencyConverter.Core.Utilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Core.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly ICurrencyProviderFactory _providerFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CurrencyService> _logger;
        private readonly HashSet<string> _restrictedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TRY", "PLN", "THB", "MXN"
        };

        public CurrencyService(
            ICurrencyProviderFactory providerFactory,
            IMemoryCache cache,
            ILogger<CurrencyService> logger)
        {
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                // Remove restricted currencies from the response
                foreach (var restrictedCurrency in _restrictedCurrencies)
                {
                    rates.Rates.Remove(restrictedCurrency);
                }

                // Cache for 1 hour
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

                // Cache for 1 hour
                _cache.Set(cacheKey, conversionData, TimeSpan.FromHours(1));
            }
            else
            {
                _logger.LogInformation("Cache hit for conversion from {FromCurrency} to {ToCurrency}",
                    request.FromCurrency, request.ToCurrency);
            }

            var rate = conversionData.Rates[request.ToCurrency];
            var convertedAmount = request.Amount * rate;

            return new CurrencyConversionResponse
            {
                Amount = request.Amount,
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                ConvertedAmount = convertedAmount,
                Rate = rate,
                Date = conversionData.Date
            };
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

                // Cache for 24 hours as historical data doesn't change
                _cache.Set(cacheKey, historicalData, TimeSpan.FromHours(24));
            }
            else
            {
                _logger.LogInformation("Cache hit for historical rates with base currency {BaseCurrency} from {StartDate} to {EndDate}",
                    request.BaseCurrency, request.StartDate.ToString("yyyy-MM-dd"), request.EndDate.ToString("yyyy-MM-dd"));
            }

            // Remove restricted currencies from all historical rates
            foreach (var date in historicalData.Keys)
            {
                foreach (var restrictedCurrency in _restrictedCurrencies)
                {
                    historicalData[date].Remove(restrictedCurrency);
                }
            }

            // Convert to list for pagination
            var allRates = historicalData
                .Select(kvp => new HistoricalRate
                {
                    Date = kvp.Key,
                    BaseCurrency = request.BaseCurrency,
                    Rates = kvp.Value
                })
                .OrderByDescending(r => r.Date)
                .ToList();

            // Apply pagination
            var totalCount = allRates.Count;
            var skip = (request.Page - 1) * request.PageSize;
            var paginatedRates = allRates
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();

            return new PaginatedResponse<HistoricalRate>
            {
                Items = paginatedRates,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
