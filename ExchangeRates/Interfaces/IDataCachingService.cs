using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExchangeRates.Models;

namespace ExchangeRates.Interfaces
{
    /// <summary>
    /// Interface that defines methods for caching euro exchanges data
    /// </summary>
    public interface IDataCachingService
    {
        /// <summary>
        /// <para>Method that gets euro exchanges to given currencies for full time period</para>
        /// </summary>
        /// <param name="currencyCodes">list of needed currencies</param>
        /// <param name="startDate">first correct datetime</param>
        /// <param name="endDate">last datetime</param>
        /// <returns>list of complete curriencies</returns>
        Task<IEnumerable<EuroExchange>> GetExchanges(
            List<string> currencyCodes,
            DateTime startDate,
            DateTime endDate);

        /// <summary>
        /// Method that stores already downloaded euro exchanges
        /// </summary>
        /// <param name="euroExchanges">euro exchanges that will be stored</param>
        Task StoreEuroExchanges(
            IEnumerable<EuroExchange> euroExchanges);

        /// <summary>
        /// Method that stores occurs of banking holidays
        /// </summary>
        /// <param name="bankingHolidays">banking holidays that will be stored</param>
        void StoreBankingHolidays(
            List<BankingHoliday> bankingHolidays);
    }
}