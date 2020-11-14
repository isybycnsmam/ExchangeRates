using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ExchangeRates.Interfaces;
using ExchangeRates.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Services
{
    /// <summary>
    /// Service that implements ICaschingEuroRates by using in memory database
    /// </summary>
    public class InMemCaschingEuroRatesService : ICaschingEuroRatesService
    {
        private readonly ExchangesContext _exchangesContext;

        public InMemCaschingEuroRatesService(
            ExchangesContext exchangesContext)
        {
            _exchangesContext = exchangesContext;
        }

        /// <summary>
        /// <para>Method that gets currencies from db that has complete info for time in between</para>
        /// </summary>
        /// <param name="currencies">list of needed currencies</param>
        /// <param name="startDate">first correct datetime</param>
        /// <param name="endDate">last datetime</param>
        /// <returns>list of complete curriencies</returns>
        public async Task<IEnumerable<EuroExchange>> Get(
            List<string> currencies,
            DateTime startDate,
            DateTime endDate)
        {
            // filter for expressing time in between
            Expression<Func<EuroExchange, bool>> datePredicate =
                e => e.Date >= startDate && e.Date <= endDate;

            // get curriences codes that has complete information for given dates
            var currenciesWithCount = await _exchangesContext.EuroExchanges
                .Where(datePredicate)
                .Where(e => currencies.Contains(e.Currency))
                .GroupBy(e => e.Currency)
                .Select(e => new { Code = e.Key, Count = e.Count() })
                .ToListAsync();

            var daysBetween = getDaysBetween(startDate, endDate);
            var completeCurrencies = currenciesWithCount
                .Where(e => e.Count == daysBetween)
                .Select(e => e.Code);

            // get already stored exchanges
            var exchangeRates = await _exchangesContext.EuroExchanges
                .Where(datePredicate)
                .Where(e => completeCurrencies.Contains(e.Currency))
                .ToListAsync();

            return exchangeRates;
        }

        public async Task Store(
            IEnumerable<EuroExchange> euroRates)
        {
            var givenEuroRatesKeys = euroRates.Select(e => $"{e.Currency}:{e.Date}").ToList();

            var existingEuroRates = await _exchangesContext.EuroExchanges
                .Where(e => givenEuroRatesKeys.Contains($"{e.Currency}:{e.Date}"))
                .Select(e => $"{e.Currency}:{e.Date}")
                .ToListAsync();

            var nonExistingEuroRates = euroRates.Where(e => existingEuroRates.Contains($"{e.Currency}:{e.Date}") == false);

            await _exchangesContext.EuroExchanges.AddRangeAsync(nonExistingEuroRates);

            await _exchangesContext.SaveChangesAsync();
        }

        /// <summary>
        /// Method that gets number of days(except weekend days) between two dates
        /// </summary>
        /// <param name="startDate">first day date</param>
        /// <param name="endDate">last day date</param>
        /// <returns>int that is numbers of days beetween given dates</returns>
        private int getDaysBetween(DateTime startDate, DateTime endDate)
        {
            var weekendDays = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
            var daysCount = 0;
            while (startDate <= endDate)
            {
                if (weekendDays.Contains(startDate.DayOfWeek) == false)
                {
                    daysCount++;
                }
                startDate = startDate.AddDays(1);
            }
            return daysCount;
        }
    }
}