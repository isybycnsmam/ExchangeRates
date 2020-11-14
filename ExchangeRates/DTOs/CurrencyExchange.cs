using System;

namespace ExchangeRates.DTOs
{
    public sealed class CurrencyExchange
    {
        public DateTime Date { get; set; }
        public string CurrencyFrom { get; set; }
        public string CurrencyTo { get; set; }
        public double ExchangeRate { get; set; }
    }
}