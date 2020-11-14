using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExchangeRates.Models;

namespace ExchangeRates.Interfaces
{
    /// <summary>
    /// Interface that defines methods for casching data
    /// </summary>
    public interface ICaschingEuroRatesService
    {
        /// <summary>
        /// <para>Method that gets euro rates to given currencies for full time period</para>
        /// </summary>
        /// <param name="currencies">list of needed currencies</param>
        /// <param name="startDate">first correct datetime</param>
        /// <param name="endDate">last datetime</param>
        /// <returns>list of complete curriencies</returns>
        Task<IEnumerable<EuroExchange>> Get(
            List<string> currencies,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="euroRates"></param>
        /// <returns></returns>            
        Task Store(
            IEnumerable<EuroExchange> euroRates);
    }
}