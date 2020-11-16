using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExchangeRates.DTOs;
using ExchangeRates.Interfaces;
using ExchangeRates.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRates.Services
{
    /// <summary>
    /// Service for getting and casching exchange rates 
    /// </summary>
    public sealed class CurrenciesService
    {
        private readonly ILogger _logger;
        private readonly IExternalSourceClient _externalApiClient;
        private readonly IDataCachingService _dataCachingService;

        public CurrenciesService(
            ILogger<CurrenciesService> logger,
            IExternalSourceClient externalApiClient,
            IDataCachingService dataCachingService,
            ExchangesContext exchangesContext)
        {
            _logger = logger;
            _externalApiClient = externalApiClient;
            _dataCachingService = dataCachingService;
        }


        /// <summary>
        /// Method that gets all rates from memory or from external sources(next saves them for later) 
        /// and then builds currency exchanges based on rates 
        /// </summary>
        /// <param name="currencyCodes">requested codes to be exchanged</param>
        /// <param name="startDate">first day</param>
        /// <param name="endDate">last day</param>
        /// <returns>generated List of CurrencyExchanges</returns>
        public async Task<List<CurrencyExchangeDTO>> GetExchanges(
            List<KeyValuePair<string, string>> currencyCodes,
            DateTime startDate,
            DateTime endDate)
        {
            // get 3 days from the past to avoid missing days
            var fixedNeededDate = substractWorkingDaysFromDate(startDate, 3);

            // get all nessesary currencies
            var nessessaryCurrencies = currencyCodes
                .SelectMany(e => new string[] { e.Key, e.Value })
                .Distinct()
                .Where(e => e != "EUR")
                .ToList();

            var neededEuroExchanges = await getNeededEuroExchangesPerDay(nessessaryCurrencies, fixedNeededDate, endDate);

            return generateCurrencyExchanges(currencyCodes, neededEuroExchanges, startDate, endDate).ToList();
        }

        /// <summary>
        /// Method that gets euro exchanges and then if some is not downloaded saves them for later
        /// </summary>
        /// <param name="nessessaryCurrencies">needed codes</param>
        /// <param name="fixedNeededDate">first euro exchange date</param>
        /// <param name="endDate">last euro exchange date</param>
        /// <returns>Dictionary of dates and list of euro exchanges for this day</returns>
        private async Task<Dictionary<DateTime, List<EuroExchange>>> getNeededEuroExchangesPerDay(
            List<string> nessessaryCurrencies,
            DateTime fixedNeededDate,
            DateTime endDate)
        {
            // add stored exchanges
            var euroRates = await _dataCachingService.GetExchanges(nessessaryCurrencies, fixedNeededDate, endDate);

            // remove all known curriencies from those that needs to by downloaded
            nessessaryCurrencies.RemoveAll(currency => euroRates.Any(e => e.Currency == currency));

            // add new exchanges and save them
            if (nessessaryCurrencies.Count > 0)
            {
                var downloadedEuroExchanges = await _externalApiClient.GetExchanges(nessessaryCurrencies, fixedNeededDate, endDate);
                await _dataCachingService.StoreEuroExchanges(downloadedEuroExchanges);
                euroRates = euroRates.Concat(downloadedEuroExchanges);
            }

            // group exchanges by date
            var avaliableCurrenciesPerDay = euroRates
                .GroupBy(e => e.Date)
                //.Select(e => new KeyValuePair<DateTime, List<EuroExchange>>(e.Key, e.ToList()))
                .ToDictionary(e => e.Key, e => e.ToList());

            return avaliableCurrenciesPerDay;
        }

        /// <summary>
        /// Method that generates currency exchanges from currency codes list and euro rates
        /// </summary>
        /// <param name="currencyCodes">requested codes to be exchanged</param>
        /// <param name="euroExchangesPerDay">euro exchanges  to all currencies</param>
        /// <param name="startDate">first day</param>
        /// <param name="endDate">last day</param>
        /// <returns>generated IEnumerable of CurrencyExchanges</returns>
        private IEnumerable<CurrencyExchangeDTO> generateCurrencyExchanges(
            List<KeyValuePair<string, string>> currencyCodes,
            Dictionary<DateTime, List<EuroExchange>> euroExchangesPerDay,
            DateTime startDate,
            DateTime endDate)
        {
            // set euro
            var euro = new EuroExchange() { Currency = "EUR", ExchangeRate = 1 };

            // get date of first usefull element in given euroexchanges
            var firstCurrencyDate = euroExchangesPerDay
                .Where(e => e.Key <= startDate)
                .Select(e => e.Key)
                .LastOrDefault();

            // get enumerators
            var currentEnumerator = euroExchangesPerDay.GetEnumerator();
            var nextEnumerator = euroExchangesPerDay.Skip(1).GetEnumerator();
            var canMoveToNext = true;

            var bankingHolidays = new List<BankingHoliday>();

            // generate currency-currency exchange rates
            var currentDay = substractWorkingDaysFromDate(startDate, 3);
            while (currentDay <= endDate)
            {
                // check if next enumerator cover next day
                if (canMoveToNext && currentDay >= nextEnumerator.Current.Key)
                {
                    currentEnumerator.MoveNext();
                    canMoveToNext = nextEnumerator.MoveNext();
                }

                if (currentDay != currentEnumerator.Current.Key &&
                    currentDay.DayOfWeek != DayOfWeek.Saturday &&
                    currentDay.DayOfWeek != DayOfWeek.Sunday &&
                    currentDay.Date < DateTime.Now)
                {
                    bankingHolidays.Add(new BankingHoliday(currentDay));
                }

                if (currentDay >= startDate)
                {

                    foreach (var codePair in currencyCodes)
                    {
                        var fromEuroRate = codePair.Key == "EUR" ? euro : currentEnumerator.Current.Value?.FirstOrDefault(e => e.Currency == codePair.Key);
                        var toEuroRate = codePair.Value == "EUR" ? euro : currentEnumerator.Current.Value?.FirstOrDefault(e => e.Currency == codePair.Value);

                        if (fromEuroRate != null && toEuroRate != null)
                        {
                            yield return CurrencyExchangeDTO.Create(fromEuroRate, toEuroRate, currentDay);
                        }
                    }
                }

                currentDay = currentDay.AddDays(1);
            }

            _dataCachingService.StoreBankingHolidays(bankingHolidays);
        }

        /// <summary>
        /// Method that substract date by given count of working days
        /// </summary>
        /// <param name="date">source date</param>
        /// <param name="workingDaysToSubtract">working days to substract</param>
        /// <returns>date time from the past</returns>
        private DateTime substractWorkingDaysFromDate(DateTime date, int workingDaysToSubtract)
        {
            var weekendDays = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };

            while (true)
            {
                if (weekendDays.Contains(date.DayOfWeek))
                {
                    var weekendDaysPassed = Array.IndexOf(weekendDays, date.DayOfWeek) + 1;
                    date = date.AddDays(-weekendDaysPassed);
                }

                if (workingDaysToSubtract > 0)
                {
                    date = date.AddDays(-1);
                }
                else
                {
                    return date;
                }

                workingDaysToSubtract--;
            }
        }
    }
}