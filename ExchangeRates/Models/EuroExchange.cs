using System;

namespace ExchangeRates.Models
{
    public sealed class EuroExchange
    {
        public DateTime Date { get; set; }
        public string Currency { get; set; }
        public double ExchangeRate { get; set; }
    }
}