using System;
using ExchangeRates.Models;

namespace ExchangeRates.DTOs
{
    /// <summary>
    /// Dto for transferring currency exchange (e.g. USD and PLN)
    /// </summary>
    public sealed class CurrencyExchangeDTO
    {
        /// <summary>
        /// Constructor that converts currencies by using their euro rates
        /// </summary>
        /// <param name="from">EuroExchange with base currency</param>
        /// <param name="to">EuroExchange with target currency</param>
        /// <param name="date">exchange date</param>
        public CurrencyExchangeDTO(
            EuroExchange from,
            EuroExchange to,
            DateTime date)
        {
            CurrencyFrom = from.Currency;
            CurrencyTo = to.Currency;
            Date = date;
            ExchangeRate = Math.Round(1 / from.ExchangeRate * to.ExchangeRate, 4);
        }

        public DateTime Date { get; set; }
        public string CurrencyFrom { get; set; }
        public string CurrencyTo { get; set; }
        public double ExchangeRate { get; set; }
    }
}