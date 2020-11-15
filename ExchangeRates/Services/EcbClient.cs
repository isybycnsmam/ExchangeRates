using ExchangeRates.Interfaces;
using ExchangeRates.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ExchangeRates.Services
{
    /// <summary>
    /// <para>Service for accessing ecb api</para>
    /// link: https://sdw-wsrest.ecb.europa.eu
    /// </summary>
    public sealed class EcbClient : IExternalSourceClient
    {
        private const string URI_SCHEME = "https://sdw-wsrest.ecb.europa.eu/service/data/EXR/D.{0}.EUR.SP00.A?startPeriod={1}&endPeriod={2}&detail=dataonly";
        private const string CSV_HEADERS = "KEY,FREQ,CURRENCY,CURRENCY_DENOM,EXR_TYPE,EXR_SUFFIX,TIME_PERIOD,OBS_VALUE";
        private readonly int CurrencyIndex, TimePeriodIndex, ObsValuevIndex;

        private readonly HttpClient _client;

        public EcbClient()
        {
            _client = new HttpClient();
            // set accept header to csv for simpler and smaller data type
            _client.DefaultRequestHeaders.Add("Accept", "text/csv");

            // set indexes once for service lifetime
            var csvHeadersArray = CSV_HEADERS.Split(',');
            CurrencyIndex = Array.IndexOf(csvHeadersArray, "CURRENCY");
            TimePeriodIndex = Array.IndexOf(csvHeadersArray, "TIME_PERIOD");
            ObsValuevIndex = Array.IndexOf(csvHeadersArray, "OBS_VALUE");
        }

        /// <summary>
        /// Method that requests ecb api for euro exchange rates for given currencies
        /// </summary>
		/// <inheritdoc />
        public async Task<IEnumerable<EuroExchange>> Get(
            List<string> currencyCodes,
            DateTime dateFrom,
            DateTime dateTo)
        {
            // create url (join needed countries together and append dates to base link) 
            var currencyCodesStr = string.Join('+', currencyCodes);
            var url = string.Format(URI_SCHEME,
                currencyCodesStr,
                dateFrom.ToString("yyyy-MM-dd"),
                dateTo.ToString("yyyy-MM-dd"));

            // request ecb api
            var responseString = await _client.GetStringAsync(url);

            // convert data
            return parseCsvData(responseString).ToList();
        }

        /// <summary>
        /// <para>Method that parse csv string to IEnumerable of euro exchanges</para>
        /// If header (first line where are column definitions) is not exactly as CSV_HEADERS method returns an empty array
        /// </summary>
        /// <param name="source">source string(csv)</param>
        /// <returns>IEnumerable of euro exchanges</returns>
        private IEnumerable<EuroExchange> parseCsvData(string source)
        {
            var lines = source.Replace("\r", "").Split('\n');
            // check if first line is correct
            if (lines.FirstOrDefault() == CSV_HEADERS)
            {
                // ignore first line (headers)
                foreach (var line in lines.Skip(1))
                {
                    var fields = line.Split(',');
                    var currency = fields.ElementAtOrDefault(CurrencyIndex);
                    var timePeriod = fields.ElementAtOrDefault(TimePeriodIndex); ;
                    var obsValue = fields.ElementAtOrDefault(ObsValuevIndex)?.Replace('.', ',');

                    if (string.IsNullOrEmpty(currency) == true ||
                        string.IsNullOrEmpty(timePeriod) == true ||
                        string.IsNullOrEmpty(obsValue) == true)
                    {
                        continue;
                    }

                    var currencyExchange = new EuroExchange()
                    {
                        Date = DateTime.Parse(timePeriod),
                        Currency = currency,
                        ExchangeRate = Convert.ToDouble(obsValue)
                    };

                    yield return currencyExchange;
                }
            }
        }
    }
}