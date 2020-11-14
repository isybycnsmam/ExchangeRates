using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExchangeRates.DTOs;

namespace ExchangeRates.Interfaces
{
	/// <summary>
	/// Interface for communicating with external api
	/// </summary>
	public interface IExternalApiClient
	{
		/// <summary>
        /// Method that gets curriencies exchanges with euro
        /// </summary>
        /// <param name="currencies">currencies codes list</param>
        /// <param name="dateFrom">start date</param>
        /// <param name="dateTo">end date</param>
        /// <returns>IEnumerable of currency exchanges for given currencies to euro</returns>
		Task<IEnumerable<CurrencyExchange>> RequestApi(
            List<string> currencies,
            DateTime dateFrom,
            DateTime dateTo);
	}
}