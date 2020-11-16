using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            // get all nessesary currencies
            var nessessaryCurrencies = currencyCodes
                .SelectMany(e => new string[] { e.Key, e.Value })
                .Distinct()
                .Where(e => e != "EUR")
                .ToList();
                
            // get 3 days from the past to avoid missing days
            var fixedDate = substractWorkingDaysFromDate(startDate, 3);

            // add stored exchanges
            var euroRates = new List<EuroExchange>(await _dataCachingService.Get(nessessaryCurrencies, fixedDate, endDate));

            // remove all known curriencies from those that needs to by downloaded
            nessessaryCurrencies.RemoveAll(currency => euroRates.Any(e => e.Currency == currency));
            
            // add new exchanges and save them
            if (nessessaryCurrencies.Count > 0)
            {
                var downloadedRates = await _externalApiClient.Get(nessessaryCurrencies, fixedDate, endDate);
                euroRates.AddRange(downloadedRates);
                // save none existing rates
                await _dataCachingService.StoreEuroExchanges(downloadedRates);
            }
            
            return generateCurrencyExchanges(currencyCodes, euroRates, startDate, endDate).ToList();
        }

        /// <summary>
        /// Method that generates currency exchanges from currency codes list and euro rates
        /// </summary>
        /// <param name="currencyCodes">requested codes to be exchanged</param>
        /// <param name="euroRates">euro rates to all currencies</param>
        /// <param name="startDate">first day</param>
        /// <param name="endDate">last day</param>
        /// <returns>generated IEnumerable of CurrencyExchanges</returns>
        private IEnumerable<CurrencyExchangeDTO> generateCurrencyExchanges(
            List<KeyValuePair<string, string>> currencyCodes,
            List<EuroExchange> euroRates,
            DateTime startDate,
            DateTime endDate)
        {
            // set euro
            var euro = new EuroExchange() { Currency = "EUR", ExchangeRate = 1 };

            // get date of first usefull element in given euroexchanges
            var firstCurrencyDate = euroRates
                .Where(e => e.Date <= startDate)
                .Select(e => e.Date)
                .LastOrDefault();

            // get dates thats nessesary to generate exchanges
            var avaliableCurrenciesPerDay = euroRates
                .Where(e => e.Date >= firstCurrencyDate)
                .GroupBy(e => e.Date)
                .Select(e => new KeyValuePair<DateTime, List<EuroExchange>>(e.Key, e.ToList()));

            // get enumerators
            var currentEnumerator = avaliableCurrenciesPerDay.GetEnumerator();
            var nextEnumerator = avaliableCurrenciesPerDay.Skip(1).GetEnumerator();

            // get enumerators and set them for initial locations
            currentEnumerator.MoveNext();
            var canMoveToNext = nextEnumerator.MoveNext();

            // holidays to be stored
            var bankingHolidaysList = new List<BankingHoliday>();

            // generate currency-currency exchange rates
            while (startDate <= endDate)
            {
                foreach (var codePair in currencyCodes)
                {
                    var fromEuroRate = codePair.Key == "EUR" ? euro : currentEnumerator.Current.Value?.FirstOrDefault(e => e.Currency == codePair.Key);
                    var toEuroRate = codePair.Value == "EUR" ? euro : currentEnumerator.Current.Value?.FirstOrDefault(e => e.Currency == codePair.Value);

                    if (fromEuroRate != null && toEuroRate != null)
                    {
                        yield return CurrencyExchangeDTO.Create(fromEuroRate, toEuroRate, startDate);
                    }
                }

                // add 1 day and check if next enumerator cover its date
                startDate = startDate.AddDays(1);
                if (canMoveToNext && startDate >= nextEnumerator.Current.Key)
                {
                    currentEnumerator.MoveNext();
                    canMoveToNext = nextEnumerator.MoveNext();
                }
                else if (startDate.DayOfWeek != DayOfWeek.Saturday &&
                        startDate.DayOfWeek != DayOfWeek.Sunday &&
                        startDate <= endDate)
                {
                    bankingHolidaysList.Add(new BankingHoliday(startDate));
                }
            }

            _dataCachingService.StoreBankingHolidays(bankingHolidaysList);
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