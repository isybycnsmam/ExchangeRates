using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace ExchangeRates.Controllers
{
	public class CurrenciesController : Controller
	{
		public IActionResult Get(
			Dictionary<string, string> currencyCodes, 
			DateTime startDate, 
			DateTime endDate, 
			string apiKey)
		{
			return Ok();
		}
	}
}