using System;
using System.ComponentModel.DataAnnotations;

namespace ExchangeRates.Models
{
    /// <summary>
    /// Db model for storing dates that are banking holidays
    /// </summary>
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