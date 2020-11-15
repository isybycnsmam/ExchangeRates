using ExchangeRates.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExchangeRates.Controllers
{
    [ApiController]
    public class CurrenciesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly CurrenciesService _currenciesService;
        private const string CURRENCY_CODE_REGEX = @"^[A-Z]{3}$";

        public CurrenciesController(
            ILogger<CurrenciesController> logger,
            CurrenciesService currenciesService)
        {
            _logger = logger;
            _currenciesService = currenciesService;
        }

        [HttpGet("/get")]
        public async Task<IActionResult> Get(
            [FromQuery, BindRequired] Dictionary<string, string> currencyCodes,
            [FromQuery, BindRequired] DateTime startDate,
            [FromQuery, BindRequired] DateTime endDate,
            [FromQuery, BindRequired] string apiKey)
        {
            if (startDate > DateTime.Now)
            {
                return NotFound();
            }
            else if (startDate > endDate)
            {
                return BadRequest("Start date is greater than End date");
            }

            foreach (var codesPair in currencyCodes)
            {
                if (Regex.IsMatch(codesPair.Key, CURRENCY_CODE_REGEX) == false ||
                    Regex.IsMatch(codesPair.Value, CURRENCY_CODE_REGEX) == false)
                {
                    return BadRequest($"Pair from currencyCodes({codesPair.Key},{codesPair.Value}) is invalid");
                }
            }

            var currencyExchanges = await _currenciesService.GetEchanges(currencyCodes.ToList(), startDate, endDate);

            return Ok(currencyExchanges);
        }
    }
}