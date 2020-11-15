using System.Threading.Tasks;
using ExchangeRates.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExchangeRates.Controllers
{
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IApiKeyService _apiKeyService;

        public TokenController(
            ILogger<TokenController> logger,
            IApiKeyService apiKeyService)
        {
            _logger = logger;
            _apiKeyService = apiKeyService;
        }

        [HttpGet("/generate")]
        public async Task<IActionResult> Index()
        {
			var apiKey = await _apiKeyService.Generate();
            return Ok(apiKey);
        }
    }
}