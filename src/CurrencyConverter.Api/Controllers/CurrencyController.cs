using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace CurrencyConverter.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(ICurrencyService currencyService, ILogger<CurrencyController> logger)
        {
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("rates")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetLatestRates([Required][FromQuery] string baseCurrency)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var clientId = User.FindFirst("ClientId")?.Value ?? "Unknown";

            try
            {
                if (string.IsNullOrEmpty(baseCurrency))
                {
                    return BadRequest("Base currency is required");
                }

                var restrictedCurrencies = new[] { "TRY", "PLN", "THB", "MXN" };
                if (Array.Exists(restrictedCurrencies, c => c.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest($"Currency {baseCurrency} is restricted and cannot be used");
                }

                var result = await _currencyService.GetLatestRatesAsync(baseCurrency);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Request: GET /api/currency/rates | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 200 | Time: {ElapsedMs}ms",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Request: GET /api/currency/rates | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 400 | Time: {ElapsedMs}ms | Error: {ErrorMessage}",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds, ex.Message);

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Request: GET /api/currency/rates | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 500 | Time: {ElapsedMs}ms",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds);

                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpGet("convert")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> ConvertCurrency(
            [Required][FromQuery] decimal amount,
            [Required][FromQuery] string fromCurrency,
            [Required][FromQuery] string toCurrency)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var clientId = User.FindFirst("ClientId")?.Value ?? "Unknown";

            try
            {
                if (amount <= 0)
                {
                    return BadRequest("Amount must be greater than zero");
                }

                if (string.IsNullOrEmpty(fromCurrency) || string.IsNullOrEmpty(toCurrency))
                {
                    return BadRequest("Source and target currencies must be specified");
                }

                var restrictedCurrencies = new[] { "TRY", "PLN", "THB", "MXN" };
                if (Array.Exists(restrictedCurrencies, c => c.Equals(fromCurrency, StringComparison.OrdinalIgnoreCase)) ||
                    Array.Exists(restrictedCurrencies, c => c.Equals(toCurrency, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest("Restricted currencies cannot be used in conversion");
                }

                var request = new CurrencyConversionRequest
                {
                    Amount = amount,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency
                };

                var result = await _currencyService.ConvertCurrencyAsync(request);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Request: GET /api/currency/convert | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 200 | Time: {ElapsedMs}ms",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Request: GET /api/currency/convert | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 400 | Time: {ElapsedMs}ms | Error: {ErrorMessage}",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds, ex.Message);

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Request: GET /api/currency/convert | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 500 | Time: {ElapsedMs}ms",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds);

                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpGet("historical")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetHistoricalRates(
            [Required][FromQuery] string baseCurrency,
            [Required][FromQuery] DateTime startDate,
            [Required][FromQuery] DateTime endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var clientId = User.FindFirst("ClientId")?.Value ?? "Unknown";

            try
            {
                if (string.IsNullOrEmpty(baseCurrency))
                {
                    return BadRequest("Base currency is required");
                }

                var restrictedCurrencies = new[] { "TRY", "PLN", "THB", "MXN" };
                if (Array.Exists(restrictedCurrencies, c => c.Equals(baseCurrency, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest($"Currency {baseCurrency} is restricted and cannot be used");
                }

                if (startDate > endDate)
                {
                    return BadRequest("Start date must be before or equal to end date");
                }

                if (page < 1)
                {
                    page = 1;
                }

                if (pageSize < 1)
                {
                    pageSize = 10;
                }

                var request = new HistoricalRatesRequest
                {
                    BaseCurrency = baseCurrency,
                    StartDate = startDate,
                    EndDate = endDate,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _currencyService.GetHistoricalRatesAsync(request);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Request: GET /api/currency/historical | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 200 | Time: {ElapsedMs}ms",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "Request: GET /api/currency/historical | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 400 | Time: {ElapsedMs}ms | Error: {ErrorMessage}",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds, ex.Message);

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Request: GET /api/currency/historical | Client: {ClientIp} | ClientId: {ClientId} | Method: GET | Response: 500 | Time: {ElapsedMs}ms",
                    clientIp, clientId, stopwatch.ElapsedMilliseconds);

                return StatusCode(500, "An error occurred while processing your request");
            }
        }
    }
}
