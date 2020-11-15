using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExchangeRates.Models;

namespace ExchangeRates.Interfaces
{
    /// <summary>
    /// Interface for communicating with external api
    /// </summary>
    public interface IExternalSourceClient
	{
		/// <summary>
        /// Method that gets euro exchanges from external source
        /// </summary>
        /// <param name="currenciesCodes">currencies codes list</param>
        /// <param name="dateFrom">start date</param>
        /// <param name="dateTo">end date</param>
        /// <returns>List of euro exchanges for given currencies</returns>
		Task<IEnumerable<EuroExchange>> Get(
            List<string> currenciesCodes,
            DateTime dateFrom,
            DateTime dateTo);
	}
}