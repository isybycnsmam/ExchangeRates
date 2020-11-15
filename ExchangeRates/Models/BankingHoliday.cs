using System;
using System.ComponentModel.DataAnnotations;

namespace ExchangeRates.Models
{
    public class BankingHoliday
    {
        public BankingHoliday(DateTime date)
        {
            Date = date;
        }

        [Key]
        public DateTime Date { get; set; }
    }
}