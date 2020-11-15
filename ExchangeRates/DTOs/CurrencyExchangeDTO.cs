using System;
using System.Text.Json.Serialization;
using ExchangeRates.Models;

namespace ExchangeRates.DTOs
{
	/// <summary>
	/// Dto for transferring currency exchange (e.g. USD and PLN)
	/// </summary>
	public sealed class CurrencyExchangeDTO
	{
		/// <summary>
		/// Method that converts currencies by using their euro rates
		/// </summary>
		/// <param name="from">EuroExchange with base currency</param>
		/// <param name="to">EuroExchange with target currency</param>
		/// <param name="date">exchange date</param>
		public static CurrencyExchangeDTO Create(
			EuroExchange from,
			EuroExchange to,
			DateTime date)
		{
			return new CurrencyExchangeDTO()
			{
				CurrencyFrom = from.Currency,
				CurrencyTo = to.Currency,
				Date = date.ToString("yyyy-MM-dd"),
				ExchangeRate = Math.Round(1 / from.ExchangeRate * to.ExchangeRate, 4)
			};
		}

		//[JsonPropertyName("date")]
		public string Date { get; set; }
		//[JsonPropertyName("from")]
		public string CurrencyFrom { get; set; }
		//[JsonPropertyName("to")]
		public string CurrencyTo { get; set; }
		//[JsonPropertyName("rate")]
		public double ExchangeRate { get; set; }
	}
}