using ExchangeRates.Interfaces;
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
    public sealed class CurrenciesController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IApiKeyService _apiKeyService;
        private readonly CurrenciesService _currenciesService;
        private const string CURRENCY_CODE_REGEX = @"^[A-Z]{3}$";

        public CurrenciesController(
            ILogger<CurrenciesController> logger,
            IApiKeyService apiKeyService,
            CurrenciesService currenciesService)
        {
            _logger = logger;
            _currenciesService = currenciesService;
            _apiKeyService = apiKeyService;
        }

        [HttpGet("/get")]
        public async Task<IActionResult> Get(
            [FromQuery, BindRequired] Dictionary<string, string> currencyCodes,
            [FromQuery, BindRequired] DateTime startDate,
            [FromQuery, BindRequired] DateTime endDate,
            [FromQuery, BindRequired] string apiKey)
        {
            if (await _apiKeyService.IsValid(apiKey) == false)
            {
                return StatusCode(403, "Invalid api key");
            }
            else if (startDate > DateTime.Now)
            {
                return NotFound("Start date is form future");
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

            var currencyExchanges = await _currenciesService.GetExchanges(currencyCodes.ToList(), startDate, endDate);

            return Ok(currencyExchanges);
        }
    }
}