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
    /// Service that implements IDataCachingService by using database
    /// </summary>
    public sealed class DbDataCachingService : IDataCachingService
    {
        private readonly ExchangesContext _exchangesContext;

        public DbDataCachingService(
            ExchangesContext exchangesContext)
        {
            _exchangesContext = exchangesContext;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<EuroExchange>> GetExchanges(
            List<string> currencyCodes,
            DateTime startDate,
            DateTime endDate)
        {
            // filter for expressing time in between timeframe
            Expression<Func<EuroExchange, bool>> datePredicate =
                e => e.Date >= startDate && e.Date <= endDate;

            // get curriences codes with counted days
            var currenciesWithCount = await _exchangesContext.EuroExchanges
                .Where(datePredicate)
                .Where(e => currencyCodes.Contains(e.Currency))
                .GroupBy(e => e.Currency)
                .Select(e => new { Code = e.Key, Count = e.Count() })
                .ToListAsync();

            // filter only those that has complete information
            var daysBetween = await getDaysBetween(startDate, endDate);
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

        /// <inheritdoc />
        public async Task StoreEuroExchanges(
            IEnumerable<EuroExchange> euroExchanges)
        {
            var givenEuroExchangesKeys = euroExchanges.Select(e => $"{e.Currency}:{e.Date}").ToList();

            var existingEuroExchanges = await _exchangesContext.EuroExchanges
                .Where(e => givenEuroExchangesKeys.Contains($"{e.Currency}:{e.Date}"))
                .Select(e => $"{e.Currency}:{e.Date}")
                .ToListAsync();

            var nonExistingEuroExchanges = euroExchanges.Where(e => existingEuroExchanges.Contains($"{e.Currency}:{e.Date}") == false).ToList();

            if (nonExistingEuroExchanges.Count() > 0)
            {
                await _exchangesContext.EuroExchanges.AddRangeAsync(nonExistingEuroExchanges);
                await _exchangesContext.SaveChangesAsync();
            }
        }

        /// <inheritdoc />
        public void StoreBankingHolidays(
            List<BankingHoliday> bankingHolidays)
        {
            if (bankingHolidays.Count < 1)
            {
                return;
            }

            // get first and last date
            DateTime? firstDate = bankingHolidays.FirstOrDefault()?.Date,
                    lastDate = bankingHolidays.LastOrDefault()?.Date;

            if (firstDate != null && lastDate != null)
            {
                var existingHolidays = _exchangesContext.BankingHolidays
                    .Where(e => e.Date >= firstDate && e.Date <= lastDate)
                    .Select(e => e.Date)
                    .ToList();

                var holidaysToAdd = bankingHolidays
                    .Where(e => existingHolidays.Contains(e.Date) == false)
                    .ToList();

                if (holidaysToAdd.Count > 0)
                {
                    _exchangesContext.BankingHolidays.AddRange(holidaysToAdd);
                    _exchangesContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Method that gets number of days(except weekend days and public banking holidays) between two dates
        /// </summary>
        /// <param name="startDate">first day date</param>
        /// <param name="endDate">last day date</param>
        /// <returns>int that is numbers of days beetween given dates</returns>
        private async Task<int> getDaysBetween(DateTime startDate, DateTime endDate)
        {
            var weekendDays = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
            var bankingHolidays = await _exchangesContext.BankingHolidays
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .Select(e => e.Date)
                .ToListAsync();

            var daysCount = 0;
            while (startDate <= endDate)
            {
                if (weekendDays.Contains(startDate.DayOfWeek) == false &&
                    bankingHolidays.Contains(startDate) == false)
                {
                    daysCount++;
                }
                startDate = startDate.AddDays(1);
            }
            return daysCount;
        }
    }
}