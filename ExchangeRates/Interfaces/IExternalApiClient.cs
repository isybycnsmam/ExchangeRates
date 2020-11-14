using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExchangeRates.Models;

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
        /// <returns>List of currency exchanges for given currencies to euro</returns>
		Task<IEnumerable<EuroExchange>> Get(
            List<string> currencies,
            DateTime dateFrom,
            DateTime dateTo);
	}
}