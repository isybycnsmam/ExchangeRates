using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExchangeRates.Controllers
{
	public class TokenController : Controller
	{
		private readonly ILogger _logger;

		public TokenController(ILogger<TokenController> logger)
		{
			_logger = logger;
		}

		public IActionResult Index()
		{
			return Ok();
		}
	}
}