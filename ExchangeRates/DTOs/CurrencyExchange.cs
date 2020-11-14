using System;
using ExchangeRates.Models;

namespace ExchangeRates.DTOs
{
    public sealed class CurrencyExchange
    {
        public CurrencyExchange(
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