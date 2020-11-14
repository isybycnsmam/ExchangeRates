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
        /// 
        /// </summary>
        /// <param name="currencyExchangeCodes"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
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

            var fixedDate = getFixedDate(startDate);

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
                await _caschingEuroRatesService.Store(downloadedRates);
            }

            return generateCurrencyExchanges(currencyExchangeCodes, euroRates, startDate, endDate).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currencyExchangeCodes"></param>
        /// <param name="euroRates"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
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
                var currencyDate = getFixedDate(startDate);
                foreach (var exchangeCodes in currencyExchangeCodes)
                {
                    var fromEuroRate = getRate(currencyDate, exchangeCodes.Key);
                    var toEuroRate = getRate(currencyDate, exchangeCodes.Value);

                    if (fromEuroRate is null || toEuroRate is null)
                    {
                        _logger.LogWarning($"Not found {exchangeCodes.Key} or {exchangeCodes.Value} currency rates");
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
        /// Method that gets first day from past that is not weekend day
        /// </summary>
        /// <param name="date">source date</param>
        /// <returns>date time from past or source date</returns>
        private DateTime getFixedDate(DateTime date)
        {
            var weekendDays = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
            if (weekendDays.Contains(date.DayOfWeek))
            {
                var daysToSubtract = Array.IndexOf(weekendDays, date.DayOfWeek) + 1;
                return date.AddDays(-daysToSubtract);
            }
            else
            {
                return date;
            }
        }
    }
}