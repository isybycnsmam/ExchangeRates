using System;

namespace ExchangeRates.Models
{
    /// <summary>
    /// Db model for storing exchange rates between euro and any currency
    /// </summary>
    public sealed class EuroExchange
    {
        public DateTime Date { get; set; }
        public string Currency { get; set; }
        public double ExchangeRate { get; set; }
    }
}