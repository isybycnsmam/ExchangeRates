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
    /// <inheritdoc />
    public sealed class CurrenciesService
    {
        private readonly ILogger _logger;
        private readonly IExternalApiClient _externalApiClient;
        private readonly ICaschingEuroRatesService _caschingEuroRatesService;

        public CurrenciesService(
            ILogger<CurrenciesService> logger,
            IExternalApiClient externalApiClient,
            ICaschingEuroRatesService caschingEuroRatesService,
            ExchangesContext exchangesContext)
        {
            _logger = logger;
            _externalApiClient = externalApiClient;
            _caschingEuroRatesService = caschingEuroRatesService;
        }


        /// <summary>
        /// Method that gets all rates from memory or from external sources(saves it for later) 
        /// and then builds exchanges based on rates 
        /// </summary>
        /// <param name="currencyExchangeCodes">requested codes to be exchanged</param>
        /// <param name="startDate">first day</param>
        /// <param name="endDate">last day</param>
        /// <returns>generated List of CurrencyExchange</returns>
        public async Task<List<CurrencyExchange>> GetEchanges(
            List<KeyValuePair<string, string>> currencyExchangeCodes,
            DateTime startDate,
            DateTime endDate)
        {
            // get all nessesary currencies
            var nessessaryCurrencies = currencyExchangeCodes
                .SelectMany(e => new string[] { e.Key, e.Value })
                .Distinct()
                .Where(e => e != "EUR")
                .ToList();

            // get 3 days from the past to avoid missing days
            var fixedDate = getFixedDate(startDate, 3);

            // create rates and add eur record
            var euroRates = new List<EuroExchange>() { new EuroExchange() { Currency = "EUR", ExchangeRate = 1 } };

            // add stored exchanges
            euroRates.AddRange(await _caschingEuroRatesService.Get(nessessaryCurrencies, fixedDate, endDate));

            // remove all known curriencies from those that needs to by downloaded
            nessessaryCurrencies.RemoveAll(currency => euroRates.Any(e => e.Currency == currency));

            // add new exchanges and save them
            if (nessessaryCurrencies.Count > 0)
            {
                var downloadedRates = await _externalApiClient.Get(nessessaryCurrencies, fixedDate, endDate);
                euroRates.AddRange(downloadedRates);
                // save none existing rates
                _ = Task.Run(() => _caschingEuroRatesService.Store(downloadedRates));
            }

            return generateCurrencyExchanges(currencyExchangeCodes, euroRates, startDate, endDate).ToList();
        }

        /// <summary>
        /// Method that generates currency exchanges from exchange codes list and euro rates
        /// </summary>
        /// <param name="currencyExchangeCodes">requested codes to be exchanged</param>
        /// <param name="euroRates">euro rates to all currencies</param>
        /// <param name="startDate">first day</param>
        /// <param name="endDate">last day</param>
        /// <returns>generated IEnumerable of CurrencyExchanges</returns>
        private IEnumerable<CurrencyExchange> generateCurrencyExchanges(
            List<KeyValuePair<string, string>> currencyExchangeCodes,
            List<EuroExchange> euroRates,
            DateTime startDate,
            DateTime endDate)
        {
            // filter for easy getting exact rate from euroRates list
            Func<DateTime, string, EuroExchange> getRate =
                (date, code) =>
                    euroRates.FirstOrDefault(euroRate =>
                        euroRate.Currency == code &&
                        (euroRate.Date == date || code == "EUR"));


            // generate currency-currency exchange rates
            while (startDate <= endDate)
            {
                var fixedDate = getFixedDate(startDate);
                var currencyDate = euroRates
                    .Skip(1)// ignore euro line
                    .Where(e => e.Date <= fixedDate)
                    .OrderByDescending(e => e.Date)
                    .FirstOrDefault()?.Date;

                if (currencyDate is null)
                {
                    _logger.LogWarning($"Data not found for date: {startDate}");
                    continue;// ignore this data :/
                }

                foreach (var exchangeCodes in currencyExchangeCodes)
                {
                    var fromEuroRate = getRate(currencyDate.Value, exchangeCodes.Key);
                    var toEuroRate = getRate(currencyDate.Value, exchangeCodes.Value);

                    if (fromEuroRate is null || toEuroRate is null)
                    {
                        _logger.LogWarning($"Not found {exchangeCodes.Key} or {exchangeCodes.Value} currency rates for day {startDate}");
                    }
                    else
                    {
                        yield return new CurrencyExchange(fromEuroRate, toEuroRate, startDate);
                    }
                }

                startDate = startDate.AddDays(1);
            }
        }

        /// <summary>
        /// <para>Method that gets first day from past that is not weekend day</para>
        /// and optionally substracts date by a specyci number of working days
        /// </summary>
        /// <param name="date">source date</param>
        /// <param name="daysToSubtract">source date</param>
        /// <returns>date time from past or source date</returns>
        private DateTime getFixedDate(DateTime date, int workingDaysToSubtract = 0)
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