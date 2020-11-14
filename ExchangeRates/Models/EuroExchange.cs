using System;
using System.ComponentModel.DataAnnotations;

namespace ExchangeRates.Models
{
    public sealed class EuroExchange
    {
        [Key]
        public DateTime Date { get; set; }
        public string Currency { get; set; }
        public double ExchangeRate { get; set; }
    }
}