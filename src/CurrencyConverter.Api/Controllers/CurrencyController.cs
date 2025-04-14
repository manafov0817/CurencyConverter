using CurrencyConverter.Core.Interfaces;
using CurrencyConverter.Core.Models.Currency;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        }

        [HttpGet("rates")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> GetLatestRates([Required][FromQuery] string baseCurrency)
        {
            if (string.IsNullOrEmpty(baseCurrency))
            {
                return BadRequest("Base currency is required");
            }

            var result = await _currencyService.GetLatestRatesAsync(baseCurrency);
            return Ok(result);
        }

        [HttpGet("convert")]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> ConvertCurrency(
            [Required][FromQuery] decimal amount,
            [Required][FromQuery] string fromCurrency,
            [Required][FromQuery] string toCurrency)
        {
            if (amount <= 0)
            {
                return BadRequest("Amount must be greater than zero");
            }

            if (string.IsNullOrEmpty(fromCurrency) || string.IsNullOrEmpty(toCurrency))
            {
                return BadRequest("Source and target currencies must be specified");
            }

            var request = new CurrencyConversionRequest
            {
                Amount = amount,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency
            };

            var result = await _currencyService.ConvertCurrencyAsync(request);
            return Ok(result);
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
            if (string.IsNullOrEmpty(baseCurrency))
            {
                return BadRequest("Base currency is required");
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
            return Ok(result);
        }
    }
}
